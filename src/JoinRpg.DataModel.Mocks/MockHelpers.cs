using JoinRpg.PrimitiveTypes;

static internal class MockHelpers
{
    public static int GetNextId<T>(this IEnumerable<T> entitySet)
        where T : IProjectEntityWithId => entitySet.Select(f => f.Id).DefaultIfEmpty().Max() + 1;
}
