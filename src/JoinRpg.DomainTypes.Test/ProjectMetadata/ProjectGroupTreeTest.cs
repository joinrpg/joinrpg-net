using JoinRpg.DomainTypes.ProjectMetadata;
using static JoinRpg.DomainTypes.Test.ProjectInfoFixture;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

/// <summary>
/// <see cref="ProjectGroupTree"/> — дерево групп проекта и всё, что из его топологии выводится.
/// Раньше <c>ResponsibleMasterRules</c> и <c>AllowToSetGroups</c> считались в DAL и проверялись
/// только интеграционно; здесь они проверяются напрямую.
/// </summary>
public class ProjectGroupTreeTest
{
    private static readonly UserIdentification Master1 = new(777);
    private static readonly UserIdentification Master2 = new(778);

    [Fact]
    public void ResponsibleMasterRulesShouldContainOnlyGroupsWithMaster()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [1], [3] = [1] },
            responsibleMasterByGroup: new Dictionary<int, UserIdentification> { [2] = Master1 });

        tree.ResponsibleMasterRules.ShouldHaveSingleItem().Id.ShouldBe(GroupId(2));
    }

    [Fact]
    public void ResponsibleMasterRulesShouldPutNestedGroupFirst()
    {
        // 3 вложена в 2, значит её правило более специфичное и должно сработать раньше.
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [1], [3] = [2] },
            responsibleMasterByGroup: new Dictionary<int, UserIdentification> { [2] = Master1, [3] = Master2 });

        tree.ResponsibleMasterRules.Select(g => g.Id).ShouldBe([GroupId(3), GroupId(2)]);
    }

    [Fact]
    public void ResponsibleMasterRulesShouldPutDeeplyNestedGroupFirst()
    {
        // Родство не только прямое: 4 лежит в 3, которая лежит в 2.
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [1], [3] = [2], [4] = [3] },
            responsibleMasterByGroup: new Dictionary<int, UserIdentification> { [2] = Master1, [4] = Master2 });

        tree.ResponsibleMasterRules.Select(g => g.Id).ShouldBe([GroupId(4), GroupId(2)]);
    }

    [Fact]
    public void ResponsibleMasterRulesOfUnrelatedGroupsShouldBeStable()
    {
        // Группы 2 и 3 друг с другом не связаны — порядок между ними произвольный,
        // но обязан быть детерминированным, иначе ответственный мастер «прыгает».
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [1], [3] = [1] },
            responsibleMasterByGroup: new Dictionary<int, UserIdentification> { [2] = Master1, [3] = Master2 });

        tree.ResponsibleMasterRules.Select(g => g.Id).ShouldBe([GroupId(3), GroupId(2)]);
    }

    [Fact]
    public void AllowToSetGroupsShouldBeFalseWhenOnlyRootGroup()
    {
        var tree = MakeGroupTree(new Dictionary<int, int[]>());

        tree.AllowToSetGroups.ShouldBeFalse();
    }

    [Fact]
    public void AllowToSetGroupsShouldBeTrueWhenRegularGroupExists()
    {
        var tree = MakeGroupTree(new Dictionary<int, int[]> { [2] = [1] });

        tree.AllowToSetGroups.ShouldBeTrue();
    }

    [Fact]
    public void AllowToSetGroupsShouldIgnoreInactiveGroups()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [1] },
            isActiveByGroup: new Dictionary<int, bool> { [2] = false });

        tree.AllowToSetGroups.ShouldBeFalse();
    }

    [Fact]
    public void AllowToSetGroupsShouldIgnoreSpecialGroups()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [1], [3] = [2] },
            types: new Dictionary<int, CharacterGroupType>
            {
                [2] = CharacterGroupType.SpecialToField,
                [3] = CharacterGroupType.SpecialToValue,
            });

        tree.AllowToSetGroups.ShouldBeFalse();
    }

    [Fact]
    public void GetChildGroupsIncludingThisShouldReturnSubtree()
    {
        var tree = MakeGroupTree(new Dictionary<int, int[]> { [2] = [1], [3] = [2] });

        tree.GetChildGroupIdsIncludingThis(GroupId(2)).ShouldBe([GroupId(2), GroupId(3)]);
    }

    [Fact]
    public void GetParentGroupIdsIncludingThisShouldReturnPathToRoot()
    {
        var tree = MakeGroupTree(new Dictionary<int, int[]> { [2] = [1], [3] = [2] });

        tree.GetParentGroupIdsIncludingThis(GroupId(3))
            .ShouldBe([GroupId(3), GroupId(2), RootGroupId], ignoreOrder: true);
    }

    [Fact]
    public void UnknownGroupShouldNotBreakTraversal()
    {
        var tree = MakeGroupTree(new Dictionary<int, int[]> { [2] = [1] });
        var unknown = GroupId(100500);

        tree.Contains(unknown).ShouldBeFalse();
        tree.GetGroupByIdOrDefault(unknown).ShouldBeNull();
        tree.GetParentGroupIdsIncludingThis(unknown).ShouldBeEmpty();
        tree.GetChildGroupsIncludingThis(unknown).ShouldBeEmpty();
    }
}
