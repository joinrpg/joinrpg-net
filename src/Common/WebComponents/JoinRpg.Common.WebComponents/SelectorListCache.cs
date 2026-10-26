using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

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
/// Синглтоном его делать нельзя: клиенты отдают разным пользователям разные данные, а внутри одного scope
/// пользователь один.
/// </para>
/// </remarks>
public sealed class SelectorListCache : IDisposable
{
    private readonly ConcurrentDictionary<(Type ItemType, string Key), Lazy<Task<object>>> entries = new();
    private readonly NavigationManager navigationManager;

    /// <param name="navigationManager">
    /// Источник переходов, на которых кэш сбрасывается. На сервере при пререндере переходов не бывает.
    /// </param>
    public SelectorListCache(NavigationManager navigationManager)
    {
        this.navigationManager = navigationManager;
        navigationManager.LocationChanged += OnLocationChanged;
    }

    /// <summary>
    /// Вернуть список по ключу, загрузив его через <paramref name="load"/>, если его ещё нет.
    /// </summary>
    /// <param name="key">
    /// Ключ должен однозначно определять результат: префикс сущности и все аргументы запроса,
    /// например <c>$"characters:{projectId.Value}:{listType}"</c>.
    /// </param>
    /// <param name="load">Загрузка списка. Вызывается не больше одного раза на ключ, если не упала.</param>
    /// <returns>Список общий для всех селекторов с этим ключом, поэтому только для чтения.</returns>
    public async Task<IReadOnlyList<TItem>> GetOrLoad<TItem>(string key, Func<Task<IEnumerable<TItem>>> load)
    {
        var cacheKey = (typeof(TItem), key);
        var entry = entries.GetOrAdd(cacheKey, _ => new Lazy<Task<object>>(() => Load(load)));
        try
        {
            return (IReadOnlyList<TItem>)await entry.Value;
        }
        catch
        {
            // Удаляем только ту запись, которая упала: её мог уже заменить повторный вызов.
            _ = entries.TryRemove(new KeyValuePair<(Type, string), Lazy<Task<object>>>(cacheKey, entry));
            throw;
        }
    }

    /// <summary>
    /// Забыть все списки. Для страниц, которые сами меняют данные и перерисовываются без перезагрузки:
    /// новые селекторы после этого увидят свежий список (уже стоящие держат свой).
    /// </summary>
    public void Clear() => entries.Clear();

    /// <inheritdoc />
    public void Dispose() => navigationManager.LocationChanged -= OnLocationChanged;

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => Clear();

    private static async Task<object> Load<TItem>(Func<Task<IEnumerable<TItem>>> load)
    {
        TItem[] items = [.. await load()];
        return new ReadOnlyCollection<TItem>(items);
    }
}
