using JoinRpg.DomainTypes.ProjectMetadata;
using static JoinRpg.DomainTypes.Test.ProjectInfoFixture;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

/// <summary>
/// Восстановленная скрытая группа открывается, когда в неё восстановлен видимый всем персонаж (#5677),
/// не нарушая правила публичного пути (#4878).
/// </summary>
public class RestoredGroupPublicationTest
{
    private const int Root = 1;

    private static CharacterGroupIdentification G(int id) => new(ProjectId, id);

    private static IReadOnlyList<int> Publish(ProjectGroupTree tree, int[] characterGroups, params int[] restored)
        => [.. RestoredGroupPublication
            .GroupsToPublish(tree, [.. characterGroups.Select(G)], restored.Select(G).ToHashSet())
            .Select(g => g.CharacterGroupId)];

    [Fact]
    public void HiddenRestoredGroupUnderRootIsPublished()
    {
        var tree = MakeGroupTree(new Dictionary<int, int[]> { [2] = [Root] }, isPublicByGroup: new Dictionary<int, bool> { [2] = false });

        Publish(tree, [2], restored: 2).ShouldBe([2]);
    }

    [Fact]
    public void HiddenGroupFromBackupIsNotPublished()
    {
        var tree = MakeGroupTree(new Dictionary<int, int[]> { [2] = [Root] }, isPublicByGroup: new Dictionary<int, bool> { [2] = false });

        Publish(tree, [2]).ShouldBeEmpty();
    }

    [Fact]
    public void AlreadyPublicGroupIsNotPublishedAgain()
    {
        var tree = MakeGroupTree(new Dictionary<int, int[]> { [2] = [Root] });

        Publish(tree, [2], restored: 2).ShouldBeEmpty();
    }

    [Fact]
    public void SpecialAndRootGroupsAreNotPublished()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root] },
            types: new Dictionary<int, CharacterGroupType> { [2] = CharacterGroupType.SpecialToValue },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false });

        Publish(tree, [Root, 2], restored: [Root, 2]).ShouldBeEmpty();
    }

    /// <summary>Скрытый восстановленный предок открывается раньше группы — иначе у неё нет публичного пути.</summary>
    [Fact]
    public void HiddenRestoredAncestorIsPublishedFirst()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [2] },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false, [3] = false });

        Publish(tree, [3], restored: [2, 3]).ShouldBe([2, 3]);
    }

    /// <summary>Под скрытой группой из копии публичного пути не получить — не открываем ничего.</summary>
    [Fact]
    public void UnderHiddenBackupGroupNothingIsPublished()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [2], [4] = [3] },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false, [3] = false, [4] = false });

        Publish(tree, [4], restored: [3, 4]).ShouldBeEmpty();
    }

    /// <summary>Если путь у группы уже есть, скрытых предков по другому пути не трогаем.</summary>
    [Fact]
    public void AncestorsAreNotPublishedWhenGroupAlreadyHasPath()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [Root, 2] },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false, [3] = false });

        Publish(tree, [3], restored: [2, 3]).ShouldBe([3]);
    }

    [Fact]
    public void SharedAncestorPublishedOnce()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [2], [4] = [2] },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false, [3] = false, [4] = false });

        Publish(tree, [3, 4], restored: [2, 3, 4]).ShouldBe([2, 3, 4]);
    }

    /// <summary>
    /// Предок, который открылся бы сам, но до группы от него не дойти по публичным (через скрытую группу
    /// из копии), не открывается.
    /// </summary>
    [Fact]
    public void AncestorNotOnPublicPathIsNotPublished()
    {
        // 5 — цель, родители: 2 (восстановленная) и 3 (из копии, скрытая); над 3 — 4 (восстановленная).
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root], [4] = [Root], [3] = [4], [5] = [2, 3] },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false, [3] = false, [4] = false, [5] = false });

        Publish(tree, [5], restored: [2, 4, 5]).ShouldBe([2, 5]);
    }

    [Fact]
    public void ThreeLevelsOfRestoredGroupsArePublishedTopDown()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [2], [4] = [3] },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false, [3] = false, [4] = false });

        Publish(tree, [4], restored: [2, 3, 4]).ShouldBe([2, 3, 4]);
    }

    [Fact]
    public void CycleDoesNotPublishTwice()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root, 3], [3] = [2] },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false, [3] = false });

        Publish(tree, [3], restored: [2, 3]).ShouldBe([2, 3]);
    }

    [Fact]
    public void InactiveRestoredAncestorIsNotPublished()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [2] },
            isActiveByGroup: new Dictionary<int, bool> { [2] = false },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false, [3] = false });

        Publish(tree, [3], restored: [2, 3]).ShouldBeEmpty();
    }

    /// <summary>Одна группа персонажа не открывается (над ней скрытая из копии) — другая всё равно открывается.</summary>
    [Fact]
    public void OneGroupBlockedOtherPublished()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [2], [4] = [Root] },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false, [3] = false, [4] = false });

        Publish(tree, [3, 4], restored: [3, 4]).ShouldBe([4]);
    }

    /// <summary>Сам персонаж в группе из копии, а её восстановленный предок — не повод его открывать.</summary>
    [Fact]
    public void RestoredAncestorOfBackupGroupIsNotPublished()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [2] },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false, [3] = false });

        Publish(tree, [3], restored: 2).ShouldBeEmpty();
    }
}
