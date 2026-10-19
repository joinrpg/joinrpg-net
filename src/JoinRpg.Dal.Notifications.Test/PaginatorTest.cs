using JoinRpg.Dal.CommonEfCore;
using JoinRpg.Interfaces;

namespace JoinRpg.Dal.Notifications.Test;

public class PaginatorTest
{
    private static readonly IQueryable<int> Keys = new[] { 3, 1, 5, 2, 4 }.AsQueryable();

    [Fact]
    public void AscendingStartsFromSmallest()
        => Keys.ApplyPaginationEfCore(new KeySetPagination(null, 2), k => k).ShouldBe([1, 2]);

    [Fact]
    public void AscendingContinuesAfterFrom()
        => Keys.ApplyPaginationEfCore(new KeySetPagination(2, 2), k => k).ShouldBe([3, 4]);

    [Fact]
    public void DescendingStartsFromLargest()
        => Keys.ApplyPaginationEfCore(KeySetPagination.Descending(null, 2), k => k).ShouldBe([5, 4]);

    [Fact]
    public void DescendingContinuesBeforeFrom()
        => Keys.ApplyPaginationEfCore(KeySetPagination.Descending(4, 2), k => k).ShouldBe([3, 2]);
}
