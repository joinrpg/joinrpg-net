using JoinRpg.DomainTypes.ProjectMetadata;
using static JoinRpg.DomainTypes.Test.ProjectInfoFixture;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

/// <summary>
/// Копия дерева с ещё не сохранённым изменением (<see cref="ProjectGroupTree.WithGroupChange"/>)
/// и проверка изменения поверх неё (<see cref="ProjectGroupTree.ValidateGroupChange"/>).
/// </summary>
public class ProjectGroupTreeChangeTest
{
    private const int Root = 1;

    private static ProjectGroupTree Tree(
        Dictionary<int, int[]> parents,
        Dictionary<int, bool>? isPublic = null)
        => MakeGroupTree(parents, isPublicByGroup: isPublic);

    #region Копия дерева

    [Fact]
    public void ChangedParentsAreReflectedInTopology()
    {
        var tree = Tree(new Dictionary<int, int[]> { [2] = [Root], [3] = [Root] });

        var after = tree.WithGroupChange(GroupId(3), isPublic: true, [GroupId(2)]);

        after.GetGroupById(GroupId(3)).DirectParentGroupIds.ShouldBe([GroupId(2)]);
        after.GetGroupById(GroupId(2)).DirectChildGroupIds.ShouldBe([GroupId(3)]);
        after.GetGroupById(GroupId(2)).AllChildGroups.ShouldBe([GroupId(3)]);
        after.GetGroupById(GroupId(3)).AllParentGroups.ShouldBe([GroupId(2), GroupId(Root)], ignoreOrder: true);
    }

    /// <summary>
    /// Пересчёт транзитивный: внуки переезжают вместе с группой.
    /// </summary>
    [Fact]
    public void GrandChildrenMoveWithTheGroup()
    {
        var tree = Tree(new Dictionary<int, int[]> { [2] = [Root], [3] = [Root], [4] = [3] });

        var after = tree.WithGroupChange(GroupId(3), isPublic: true, [GroupId(2)]);

        after.GetGroupById(GroupId(2)).AllChildGroups.ShouldBe([GroupId(3), GroupId(4)], ignoreOrder: true);
        after.GetGroupById(GroupId(4)).AllParentGroups
            .ShouldBe([GroupId(3), GroupId(2), GroupId(Root)], ignoreOrder: true);
    }

    [Fact]
    public void OriginalTreeIsNotChanged()
    {
        var tree = Tree(new Dictionary<int, int[]> { [2] = [Root], [3] = [Root] });

        _ = tree.WithGroupChange(GroupId(3), isPublic: false, [GroupId(2)]);

        tree.GetGroupById(GroupId(3)).DirectParentGroupIds.ShouldBe([GroupId(Root)]);
        tree.GetGroupById(GroupId(3)).IsPublic.ShouldBeTrue();
    }

    [Fact]
    public void UnknownGroupIsRejected()
        => Should.Throw<ArgumentException>(
            () => Tree(new Dictionary<int, int[]> { [2] = [Root] })
                .WithGroupChange(GroupId(42), isPublic: true, [GroupId(Root)]));

    #endregion

    #region Проверка изменения

    [Fact]
    public void MakingGroupPublicUnderPrivateParentIsRejected()
    {
        var tree = Tree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [Root] },
            new Dictionary<int, bool> { [2] = false, [3] = false });

        var exception = Should.Throw<PublicGroupWithoutPublicPathException>(
            () => tree.ValidateGroupChange(GroupId(3), isPublic: true, [GroupId(2)]));

        exception.GroupNames.ShouldBe(["Group3"]);
    }

    /// <summary>
    /// Скрывая группу, мастер рвёт путь и публичной ветке под ней — это новое нарушение.
    /// </summary>
    [Fact]
    public void HidingGroupWithPublicChildIsRejected()
    {
        var tree = Tree(new Dictionary<int, int[]> { [2] = [Root], [3] = [2] });

        var exception = Should.Throw<PublicGroupWithoutPublicPathException>(
            () => tree.ValidateGroupChange(GroupId(2), isPublic: false, [GroupId(Root)]));

        exception.GroupNames.ShouldBe(["Group3"]);
    }

    [Fact]
    public void HidingGroupWithPrivateChildIsAllowed()
    {
        var tree = Tree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [2] },
            new Dictionary<int, bool> { [3] = false });

        Should.NotThrow(() => tree.ValidateGroupChange(GroupId(2), isPublic: false, [GroupId(Root)]));
    }

    [Fact]
    public void SecondPublicParentMakesChangeValid()
    {
        var tree = Tree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [Root], [4] = [Root] },
            new Dictionary<int, bool> { [2] = false, [4] = false });

        Should.NotThrow(() => tree.ValidateGroupChange(GroupId(4), isPublic: true, [GroupId(2), GroupId(3)]));
    }

    /// <summary>
    /// Главное свойство проверки: она сравнивает нарушения ДО и ПОСЛЕ. Старые нарушения в чужих
    /// ветках (а в данных они есть) не должны мешать править эту группу.
    /// </summary>
    [Fact]
    public void ExistingViolationInAnotherBranchDoesNotBlockChange()
    {
        var tree = Tree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [2], [4] = [Root] },
            new Dictionary<int, bool> { [2] = false });

        Should.NotThrow(() => tree.ValidateGroupChange(GroupId(4), isPublic: true, [GroupId(Root)]));
    }

    /// <summary>
    /// Но и чинить их не мешает: если изменение убирает старое нарушение, оно тем более проходит.
    /// </summary>
    [Fact]
    public void FixingExistingViolationIsAllowed()
    {
        var tree = Tree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [2] },
            new Dictionary<int, bool> { [2] = false });

        Should.NotThrow(() => tree.ValidateGroupChange(GroupId(3), isPublic: true, [GroupId(Root)]));

        var after = tree.WithGroupChange(GroupId(3), isPublic: true, [GroupId(Root)]);
        PublicGroupPathRule.FindViolations(after).ShouldBeEmpty();
    }

    #endregion
}
