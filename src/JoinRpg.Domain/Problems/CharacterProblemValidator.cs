using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Problems;

internal class CharacterProblemValidator(
    ICharacterProblemFilter[] filters,
    IFieldRelatedProblemFilter<CharacterInfo>[] fieldFilters
    ) : ICharacterProblemValidator
{
    private readonly ICharacterProblemFilter[] filters = ShouldBeNotEmpty(filters);

    // Второй набор проверяется отдельно: с IFieldRelatedProblemFilter снято ограничение по
    // TObject, поэтому опечатка в аргументе типа при регистрации (Character вместо CharacterInfo)
    // не ловится ни компилятором, ни контейнером. Без этой проверки страницы остались бы
    // двухсотками, а проблемы полей молча перестали бы считаться.
    private readonly IFieldRelatedProblemFilter<CharacterInfo>[] fieldFilters = ShouldBeNotEmpty(fieldFilters);

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

        return ValidateFieldsInternal(character, [.. character.GetFieldsToFill()]);
    }

    public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(CharacterInfo character, IEnumerable<ProjectFieldIdentification> fields)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(fields);

        return ValidateFieldsInternal(
            character,
            [.. character.GetFieldsToFill().Where(f => fields.Contains(f.Field.Id))]);
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

    private static T[] ShouldBeNotEmpty<T>(T[] filters)
        => filters.Length > 0
            ? filters
            : throw new InvalidOperationException(
                $"Filters {typeof(T).FullName} for type {typeof(CharacterInfo).FullName} do not exists");
}
