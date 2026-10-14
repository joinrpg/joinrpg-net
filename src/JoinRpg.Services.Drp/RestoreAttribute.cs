namespace JoinRpg.Services.Drp;

/// <summary>Откуда план восстановления взял значение.</summary>
public enum RestoreSourceKind
{
    /// <summary>Восстановленная база — на момент бэкапа.</summary>
    Database,

    /// <summary>Выгрузка мастера — на дату выгрузки.</summary>
    Export,

    /// <summary>Уведомление — на момент события.</summary>
    Notification,
}

/// <summary>Значение атрибута из одного источника.</summary>
/// <param name="At">На какой момент значение верно: бэкап, дата выгрузки или время события.</param>
public record RestoreSourcedValue<T>(T Value, RestoreSourceKind Source, DateTimeOffset At);

/// <summary>
/// Спорный атрибут: выбранное значение (самое свежее) и значения других источников, если они расходятся.
/// </summary>
/// <param name="Chosen">Что план применит по умолчанию.</param>
/// <param name="Alternatives">Отличающиеся значения других источников — мастер может выбрать одно из них.</param>
public record RestoreAttribute<T>(RestoreSourcedValue<T> Chosen, IReadOnlyList<RestoreSourcedValue<T>> Alternatives)
{
    /// <summary>Источники расходятся.</summary>
    public bool HasConflict => Alternatives.Count > 0;

    /// <summary>
    /// Самое свежее из известных значений; при равном времени — то, что передано позже.
    /// null — ни один источник ничего не знает.
    /// </summary>
    internal static RestoreAttribute<T>? Choose(params RestoreSourcedValue<T>?[] values)
    {
        var known = values.OfType<RestoreSourcedValue<T>>().ToList();
        if (known.Count == 0)
        {
            return null;
        }
        // OrderBy устойчивая — при равном времени последним остаётся переданный позже.
        var chosen = known.OrderBy(v => v.At).Last();
        IReadOnlyList<RestoreSourcedValue<T>> alternatives = [.. known
            .Where(v => !EqualityComparer<T>.Default.Equals(v.Value, chosen.Value))
            .OrderByDescending(v => v.At)
            .DistinctBy(v => v.Value)];
        return new(chosen, alternatives);
    }
}
