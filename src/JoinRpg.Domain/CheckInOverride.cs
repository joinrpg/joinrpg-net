using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain;

/// <summary>
/// Препятствие к регистрации, как его подтверждает мастер: тип проблемы и, для проблемы поля, само
/// поле. Время и текст проблемы сюда не входят — подтверждают «то же препятствие», а не тот же
/// экземпляр проблемы.
/// </summary>
public record CheckInBlockerKey(ClaimProblemType ProblemType, int? ProjectFieldId)
{
    public static CheckInBlockerKey From(ClaimProblem problem)
        => new(problem.ProblemType, (problem as FieldRelatedProblem)?.Field.Id.ProjectFieldId);

    /// <summary>
    /// Строка для формы: <c>FieldIsEmpty:123</c> или <c>MissingPassport</c>. Разбирается
    /// <see cref="Parse"/>.
    /// </summary>
    public override string ToString() => ProjectFieldId is { } fieldId ? $"{ProblemType}:{fieldId}" : ProblemType.ToString();

    public static CheckInBlockerKey Parse(string value)
    {
        var parts = value.Split(':');
        return parts.Length switch
        {
            1 => new(Enum.Parse<ClaimProblemType>(parts[0]), null),
            2 => new(Enum.Parse<ClaimProblemType>(parts[0]), int.Parse(parts[1])),
            _ => throw new FormatException($"Wrong check-in blocker key {value}"),
        };
    }
}

/// <summary>
/// Явное подтверждение «зарегистрировать, несмотря на это»: что мастер видел на форме регистрации и
/// решил пропустить. Регистрация в обход проходит, только если нынешние препятствия не шире
/// подтверждённых, — появившееся после открытия формы мастер не подтверждал.
/// </summary>
/// <param name="Problems">Незаполненные поля и данные профиля.</param>
/// <param name="FeeDue">Несданный взнос; игрок выходит без него, долг на заявке остаётся.</param>
public record CheckInOverride(IReadOnlyCollection<CheckInBlockerKey> Problems, int FeeDue);
