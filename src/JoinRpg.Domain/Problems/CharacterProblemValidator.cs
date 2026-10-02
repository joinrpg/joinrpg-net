using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Problems;

internal class CharacterProblemValidator(
    ICharacterProblemFilter[] filters,
    IFieldRelatedProblemFilter<CharacterInfo>[] fieldFilters
    ) : ICharacterProblemValidator
{
    private readonly ICharacterProblemFilter[] filters = ShouldBeNotEmpty(filters);

    public IEnumerable<ClaimProblem> Validate(CharacterInfo character, ProblemSeverity minimalSeverity = ProblemSeverity.Hint)
    {
        ArgumentNullException.ThrowIfNull(character);

        var problems = filters.SelectMany(filter => filter.GetProblems(character));
        var fieldProblems = ValidateFieldsOnly(character);

        return problems.Union(fieldProblems).Where(problem => problem.Severity >= minimalSeverity);
    }

    public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(CharacterInfo character)
    {
        ArgumentNullException.ThrowIfNull(character);

        return ValidateFieldsInternal(character, GetFields(character));
    }

    public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(CharacterInfo character, IEnumerable<ProjectFieldIdentification> fields)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(fields);

        return ValidateFieldsInternal(
            character,
            [.. GetFields(character).Where(f => fields.Contains(f.Field.Id))]);
    }

    private IEnumerable<FieldRelatedProblem> ValidateFieldsInternal(CharacterInfo character, FieldWithValue[] fieldWithValues)
    {
        // Агрегат сам является IFieldAvailabilityTarget, поэтому обёртка над сущностью
        // (CharacterItem/CharacterBulkLoader) здесь не нужна — см. ADR013.
        foreach (var fieldWithValue in fieldWithValues)
        {
            foreach (var filter in fieldFilters)
            {
                foreach (var problem in filter.CheckField(character, fieldWithValue))
                {
                    yield return problem;
                }
            }
        }
    }

    /// <summary>
    /// Поля, за которые персонаж отвечает сам.
    /// </summary>
    /// <remarks>
    /// Поля, привязанные к заявке, спрашиваем с персонажа только если заявка утверждена: пока
    /// персонаж свободен, их просто некому заполнять, и «поле не заполнено» было бы ложной
    /// проблемой.
    /// </remarks>
    private static FieldWithValue[] GetFields(CharacterInfo character)
        => [.. character.GetAllFields()
            .Where(pf => pf.Field.BoundTo == FieldBoundTo.Character || character.ApprovedClaimId is not null)];

    private static ICharacterProblemFilter[] ShouldBeNotEmpty(ICharacterProblemFilter[] filters)
        => filters.Length > 0
            ? filters
            : throw new InvalidOperationException($"Filters for type {typeof(CharacterInfo).FullName} do not exists");
}
