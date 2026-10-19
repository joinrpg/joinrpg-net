using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Interfaces;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

public class PaginatorTest
{
    private static readonly IQueryable<int> Keys = new[] { 3, 1, 5, 2, 4 }.AsQueryable();

    [Fact]
    public void AscendingStartsFromSmallest()
        => Keys.ApplyPaginationEf(new KeySetPagination(null, 2), k => k).ShouldBe([1, 2]);

    [Fact]
    public void AscendingContinuesAfterFrom()
        => Keys.ApplyPaginationEf(new KeySetPagination(2, 2), k => k).ShouldBe([3, 4]);

    [Fact]
    public void DescendingStartsFromLargest()
        => Keys.ApplyPaginationEf(KeySetPagination.Descending(null, 2), k => k).ShouldBe([5, 4]);

    [Fact]
    public void DescendingContinuesBeforeFrom()
        => Keys.ApplyPaginationEf(KeySetPagination.Descending(4, 2), k => k).ShouldBe([3, 2]);
}
