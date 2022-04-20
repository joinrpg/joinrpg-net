using JoinRpg.DataModel;

static internal class MockHelpers
{
    public static int GetNextId<T>(this IEnumerable<T> entitySet)
        where T : IProjectEntity => entitySet.Select(f => f.Id).DefaultIfEmpty().Max() + 1;
}
