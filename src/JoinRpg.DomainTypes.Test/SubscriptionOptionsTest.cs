using JoinRpg.DomainTypes.Users;

namespace JoinRpg.DomainTypes.Test;

public class SubscriptionOptionsTest
{
    private static SubscriptionOptions None => SubscriptionOptions.CreateNoneSet();
    private static SubscriptionOptions All => SubscriptionOptions.CreateAllSet();
    private static SubscriptionOptions ClaimOnly => None with { ClaimStatusChange = true };
    private static SubscriptionOptions CommentsOnly => None with { Comments = true };
    private static SubscriptionOptions ClaimAndComments => None with { ClaimStatusChange = true, Comments = true };

    [Fact]
    public void Union_WithNone_ReturnsSelf() => ClaimOnly.Union(None).ShouldBe(ClaimOnly);

    [Fact]
    public void Union_FromNone_ReturnsSame() => None.Union(ClaimOnly).ShouldBe(ClaimOnly);

    [Fact]
    public void Union_WithSelf_ReturnsSelf() => ClaimOnly.Union(ClaimOnly).ShouldBe(ClaimOnly);

    [Fact]
    public void Union_DisjointSets_CombinesFlags() =>
        ClaimOnly.Union(CommentsOnly).ShouldBe(ClaimAndComments);

    [Fact]
    public void Union_WithAll_ReturnsAll() => ClaimOnly.Union(All).ShouldBe(All);

    [Fact]
    public void Union_IsCommutative() =>
        ClaimOnly.Union(CommentsOnly).ShouldBe(CommentsOnly.Union(ClaimOnly));

    [Fact]
    public void Except_Self_ReturnsNone() => ClaimOnly.Except(ClaimOnly).ShouldBe(None);

    [Fact]
    public void Except_None_ReturnsSelf() => ClaimOnly.Except(None).ShouldBe(ClaimOnly);

    [Fact]
    public void Except_Superset_ReturnsNone() => ClaimOnly.Except(All).ShouldBe(None);

    [Fact]
    public void Except_Disjoint_ReturnsSelf() => ClaimOnly.Except(CommentsOnly).ShouldBe(ClaimOnly);

    [Fact]
    public void Except_RemovesOnlyOverlap() =>
        ClaimAndComments.Except(ClaimOnly).ShouldBe(CommentsOnly);

    [Fact]
    public void UnionThenExcept_YieldsNewFlags() =>
        ClaimOnly.Union(CommentsOnly).Except(ClaimOnly).ShouldBe(CommentsOnly);
}
