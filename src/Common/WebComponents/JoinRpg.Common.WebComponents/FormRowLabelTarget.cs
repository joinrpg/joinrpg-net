namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Связь подписи <see cref="FormRow"/> с полем ввода внутри строки. Строка каскадирует этот объект вниз,
/// поле забирает у него id (<see cref="Claim"/>), и подпись получает <c>&lt;label for&gt;</c> на это поле.
/// Пока ни одно поле id не забрало, у подписи нет <c>for</c> — она не ссылается на несуществующий элемент.
/// </summary>
public sealed class FormRowLabelTarget
{
    private static long counter;

    private readonly string generatedId;
    private readonly Action onClaimed;
    private object? owner;

    internal FormRowLabelTarget(string? fieldName, Action onClaimed)
    {
        generatedId = $"formrow-{fieldName ?? "field"}-{Interlocked.Increment(ref counter)}";
        this.onClaimed = onClaimed;
    }

    /// <summary>id поля, на которое ссылается подпись. <c>null</c> — ни одно поле его не забрало.</summary>
    public string? InputId { get; private set; }

    /// <summary>
    /// Забрать id для поля. Подпись ссылается на первое забравшее поле; остальным полям строки возвращается
    /// <c>null</c> — иначе в строке оказались бы два элемента с одним id.
    /// </summary>
    /// <param name="field">Компонент поля: повторный вызов от него же при перерисовке возвращает тот же id.</param>
    /// <param name="explicitId">Явный <c>id</c> поля, если он задан в разметке: подпись сошлётся на него.</param>
    /// <returns>id, который поле должно вывести, или <paramref name="explicitId"/>, если строка уже занята другим полем.</returns>
    public string? Claim(object field, string? explicitId = null)
    {
        if (owner is not null && !ReferenceEquals(owner, field))
        {
            return explicitId;
        }

        owner = field;
        var id = explicitId ?? generatedId;
        if (InputId != id)
        {
            InputId = id;
            // Подпись выведена раньше поля — строку надо перерисовать, чтобы у неё появился for.
            onClaimed();
        }
        return id;
    }

    /// <summary>Достать явный <c>id</c> из атрибутов, собранных через <c>CaptureUnmatchedValues</c>.</summary>
    public static string? ExplicitId(IReadOnlyDictionary<string, object>? attributes)
        => attributes?.TryGetValue("id", out var id) == true ? id?.ToString() : null;
}
