using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain.Problems.CharacterProblemFilters;

namespace JoinRpg.Domain.Problems;

public static class CharacterProblemExtensions
{
    private static IProblemFilter<Character>[] Filters { get; }

    public static IEnumerable<ClaimProblem> GetProblems([NotNull] this Character claim, ProblemSeverity minimalSeverity = ProblemSeverity.Hint)
    {
        if (claim == null)
        {
            throw new ArgumentNullException(nameof(claim));
        }

        return Filters.SelectMany(f => f.GetProblems(claim)).Where(p => p.Severity >= minimalSeverity);
    }

    static CharacterProblemExtensions()
    {
        Filters = new IProblemFilter<Character>[]
        {
    new FieldNotSetFilterCharacter(),
        };
    }

    public static bool HasProblemsForField([NotNull] this Character character, [NotNull] ProjectField field)
    {
        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        if (field == null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        return character.GetProblems().OfType<FieldRelatedProblem>().Any(fp => fp.Field == field);
    }
}
