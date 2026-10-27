using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Общий кэш списков, которые грузят обёртки над <see cref="TypedSelector{TKey, TItem}"/>.
/// Без него N одинаковых селекторов на странице (например, по строке таблицы) делают N одинаковых запросов (#5524).
/// </summary>
/// <remarks>
/// <para>
/// Хранится <see cref="Task"/>, а не готовый результат: селекторы, которые начали грузиться одновременно,
/// дожидаются одного и того же запроса. Если загрузка упала, запись выкидывается, и следующий вызов
/// попробует снова.
/// </para>
/// <para>
/// Списки живут до перехода на другую страницу. Регистрируется как scoped: на сервере scope — это запрос,
/// в WebAssembly — всё приложение, и обычный переход перезагружает страницу и создаёт пустой кэш.
/// Но программный <see cref="NavigationManager.NavigateTo(string, bool, bool)"/> без <c>forceLoad</c>
/// может обойтись без перезагрузки — поэтому кэш сбрасывается сам на каждое
/// <see cref="NavigationManager.LocationChanged"/>, чтобы следующая страница не получила устаревшие списки.
/// Если страница сама меняет данные и перерисовывается без перехода (DRP-восстановление создаёт
/// персонажей), она сбрасывает кэш через <see cref="Clear"/>.
/// </para>
/// <para>
/// Списки, загруженные при пререндере на сервере, переезжают в WebAssembly через
/// <see cref="PersistentComponentState"/> (#4119): сервер дописывает их в состояние страницы
/// (<c>&lt;persist-component-state /&gt;</c> в layout портала), а селектор после старта WebAssembly
/// берёт список оттуда и не грузит его второй раз. Делается здесь, а не через <c>[PersistentState]</c>
/// в обёртках: ключи атрибута строятся по положению компонента, а кэш знает, что списки с одинаковым
/// ключом одинаковые, и переносит каждый один раз. Переносятся только списки не длиннее
/// <see cref="MaxPersistedItems"/>: состояние кодируется в HTML дважды (base64 внутри base64) и плохо
/// сжимается, и список всех персонажей крупного проекта утяжелил бы саму страницу сильнее, чем стоит
/// второй запрос после её загрузки. Списки статических селекторов на странице с островом тоже попадают
/// в состояние — забрать их некому, но сейчас это только небольшой список мастеров.
/// </para>
/// <para>
/// Синглтоном его делать нельзя: клиенты отдают разным пользователям разные данные, а внутри одного scope
/// пользователь один.
/// </para>
/// </remarks>
public sealed class SelectorListCache : IDisposable
{
    /// <summary>Списки длиннее этого с пререндера не переносятся — клиент загрузит их сам.</summary>
    internal const int MaxPersistedItems = 200;

    private readonly ConcurrentDictionary<(Type ItemType, string Key), Entry> entries = new();
    private readonly ILogger<SelectorListCache> logger;
    private readonly NavigationManager navigationManager;
    private readonly PersistentComponentState persistentState;
    private readonly PersistingComponentStateSubscription persistingSubscription;

    /// <param name="navigationManager">
    /// Источник переходов, на которых кэш сбрасывается. На сервере при пререндере переходов не бывает.
    /// </param>
    /// <param name="persistentState">Состояние, через которое списки пререндера переезжают в WebAssembly.</param>
    /// <param name="logger">Сюда — сбои переноса: они не роняют селектор, список просто грузится заново.</param>
    public SelectorListCache(NavigationManager navigationManager, PersistentComponentState persistentState, ILogger<SelectorListCache> logger)
    {
        this.navigationManager = navigationManager;
        this.persistentState = persistentState;
        this.logger = logger;
        navigationManager.LocationChanged += OnLocationChanged;

        // Колбэк не принадлежит компоненту, поэтому режим рендера указываем явно — иначе фреймворк не сможет
        // его вывести, когда хранилищ несколько. Сохраняем только для WebAssembly: серверной интерактивности
        // в порталах нет, а статическим компонентам переносить нечего. В WebAssembly колбэк не вызывается.
        persistingSubscription = persistentState.RegisterOnPersisting(PersistLoadedLists, RenderMode.InteractiveWebAssembly);
    }

    /// <summary>
    /// Вернуть список по ключу, загрузив его через <paramref name="load"/>, если его ещё нет.
    /// </summary>
    /// <param name="key">
    /// Ключ должен однозначно определять результат: префикс сущности и все аргументы запроса,
    /// например <c>$"characters:{projectId.Value}:{listType}"</c>. Он же — ключ в состоянии пререндера,
    /// поэтому на сервере и в WebAssembly должен строиться одинаково.
    /// </param>
    /// <param name="load">Загрузка списка. Вызывается не больше одного раза на ключ, если не упала.</param>
    /// <returns>Список общий для всех селекторов с этим ключом, поэтому только для чтения.</returns>
    public async Task<IReadOnlyList<TItem>> GetOrLoad<TItem>(string key, Func<Task<IEnumerable<TItem>>> load)
    {
        var cacheKey = (typeof(TItem), key);
        var entry = entries.GetOrAdd(cacheKey, _ => CreateEntry(key, load));
        try
        {
            return (IReadOnlyList<TItem>)await entry.List.Value;
        }
        catch
        {
            // Удаляем только ту запись, которая упала: её мог уже заменить повторный вызов.
            _ = entries.TryRemove(new KeyValuePair<(Type, string), Entry>(cacheKey, entry));
            throw;
        }
    }

    /// <summary>
    /// Забыть все списки. Для страниц, которые сами меняют данные и перерисовываются без перезагрузки:
    /// новые селекторы после этого увидят свежий список (уже стоящие держат свой).
    /// </summary>
    public void Clear() => entries.Clear();

    /// <inheritdoc />
    public void Dispose()
    {
        navigationManager.LocationChanged -= OnLocationChanged;
        persistingSubscription.Dispose();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => Clear();

    private Entry CreateEntry<TItem>(string key, Func<Task<IEnumerable<TItem>>> load)
    {
        var persistKey = $"{nameof(SelectorListCache)}:{typeof(TItem).FullName}:{key}";

        // Состояние пререндера читаем внутри Lazy: GetOrAdd может вызвать фабрику записи и выбросить
        // результат, а TryTakeFromJson забирает ключ насовсем. Отдаётся оно один раз: после первой отрисовки
        // WebAssembly-хост очищает всё, что не забрали, и позже созданные селекторы грузят список сами.
        var list = new Lazy<Task<object>>(()
            => TryTakePersisted<TItem>(persistKey) is { } restored
                ? Task.FromResult<object>(new ReadOnlyCollection<TItem>(restored))
                : Load(load));

        return new Entry(list, () =>
        {
            // Незавершённые и упавшие загрузки не переносим: клиент загрузит их сам.
            if (list.IsValueCreated
                && list.Value.IsCompletedSuccessfully
                && list.Value.Result is IReadOnlyList<TItem> { Count: <= MaxPersistedItems } loaded)
            {
                try
                {
                    persistentState.PersistAsJson(persistKey, loaded);
                }
                catch (Exception exception)
                {
                    // Иначе фреймворк сбросит всё состояние страницы, включая чужое (антифорджери и т. п.).
                    logger.LogWarning(exception, "Не удалось сохранить список селектора {PersistKey} для WebAssembly", persistKey);
                }
            }
        });
    }

    private TItem[]? TryTakePersisted<TItem>(string persistKey)
    {
        try
        {
            return persistentState.TryTakeFromJson<TItem[]>(persistKey, out var restored) ? restored : null;
        }
        catch (Exception exception)
        {
            // Тип элемента перестал читаться из JSON (например, у record появился второй конструктор) —
            // селектор не должен из-за этого упасть, список загрузится обычным путём.
            logger.LogWarning(exception, "Не удалось прочитать список селектора {PersistKey} с пререндера", persistKey);
            return null;
        }
    }

    private Task PersistLoadedLists()
    {
        foreach (var entry in entries.Values)
        {
            entry.Persist();
        }
        return Task.CompletedTask;
    }

    private static async Task<object> Load<TItem>(Func<Task<IEnumerable<TItem>>> load)
    {
        TItem[] items = [.. await load()];
        return new ReadOnlyCollection<TItem>(items);
    }

    /// <param name="List">Загрузка списка (или уже готовый список, перенесённый с пререндера).</param>
    /// <param name="Persist">Записать список в состояние пререндера с правильным типом элемента.</param>
    private sealed record Entry(Lazy<Task<object>> List, Action Persist);
}
