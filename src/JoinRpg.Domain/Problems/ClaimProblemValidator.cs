using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Problems;

internal class ClaimProblemValidator(
    IClaimProblemFilter[] filters,
    IFieldRelatedProblemFilter<CharacterClaimInfo>[] fieldFilters
    ) : IClaimProblemValidator
{
    private readonly IClaimProblemFilter[] filters = ShouldBeNotEmpty(filters);

    // Второй набор проверяется отдельно: с IFieldRelatedProblemFilter снято ограничение по
    // TObject, поэтому опечатка в аргументе типа при регистрации (Claim вместо CharacterClaimInfo)
    // не ловится ни компилятором, ни контейнером. Без этой проверки страницы остались бы
    // двухсотками, а проблемы полей молча перестали бы считаться.
    private readonly IFieldRelatedProblemFilter<CharacterClaimInfo>[] fieldFilters = ShouldBeNotEmpty(fieldFilters);

    public IEnumerable<ClaimProblem> Validate(ClaimInfo context, ProblemSeverity minimalSeverity = ProblemSeverity.Hint)
    {
        ArgumentNullException.ThrowIfNull(context);

        var problems = filters.SelectMany(filter => filter.GetProblems(context));
        var fieldProblems = ValidateFieldsOnly(context);

        return problems.Union(fieldProblems).Where(problem => problem.Severity >= minimalSeverity);
    }

    public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimInfo context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ValidateFieldsInternal(context, [.. GetFieldsToFill(context)]);
    }

    public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimInfo context, IEnumerable<ProjectFieldIdentification> fields)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(fields);

        return ValidateFieldsInternal(
            context,
            [.. GetFieldsToFill(context).Where(f => fields.Contains(f.Field.Id))]);
    }

    private IEnumerable<FieldRelatedProblem> ValidateFieldsInternal(ClaimInfo context, FieldWithValue[] fieldWithValues)
    {
        // Доступность поля проверяется по персонажу: это он лежит в группах, от которых зависят
        // правила «поле доступно для этой группы». Агрегат сам является IFieldAvailabilityTarget,
        // поэтому обёртка над EF-сущностью (CharacterItem) здесь не нужна — см. ADR013.
        foreach (var fieldWithValue in fieldWithValues)
        {
            foreach (var filter in fieldFilters)
            {
                foreach (var problem in filter.CheckField(context.Character, fieldWithValue))
                {
                    yield return problem;
                }
            }
        }
    }

    /// <summary>
    /// Поля, которые на уровне этой заявки есть кому заполнять: её собственные — всегда, поля
    /// персонажа — только когда заявка утверждена. Правило живёт в агрегате, одно на сторону
    /// персонажа и сторону заявки, — см. <see cref="CharacterInfo.GetFieldsToFill"/>.
    /// </summary>
    private static IReadOnlyCollection<FieldWithValue> GetFieldsToFill(ClaimInfo context)
        => context.Character.GetFieldsToFill(context.Claim.ClaimId);

    private static T[] ShouldBeNotEmpty<T>(T[] filters)
        => filters.Length > 0
            ? filters
            : throw new InvalidOperationException(
                $"Filters {typeof(T).FullName} for type {typeof(ClaimInfo).FullName} do not exists");
}
