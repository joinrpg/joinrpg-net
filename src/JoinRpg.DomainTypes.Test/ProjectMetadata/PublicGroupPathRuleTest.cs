using JoinRpg.DomainTypes.ProjectMetadata;
using static JoinRpg.DomainTypes.Test.ProjectInfoFixture;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

/// <summary>
/// Правило #4878: у публичной группы должен быть хотя бы один публичный путь до корня.
/// Проверяется поверх готового дерева, поэтому здесь же видно, как ведут себя граф и циклы.
/// </summary>
public class PublicGroupPathRuleTest
{
    private const int Root = 1;

    private static IReadOnlyCollection<int> Violations(ProjectGroupTree tree)
        => [.. PublicGroupPathRule.FindViolations(tree).Select(group => group.Id.CharacterGroupId)];

    [Fact]
    public void AllPublicTreeHasNoViolations()
        => Violations(MakeGroupTree(new Dictionary<int, int[]> { [2] = [Root], [3] = [2] })).ShouldBeEmpty();

    [Fact]
    public void PublicGroupUnderPrivateParentIsViolation()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [2] },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false });

        Violations(tree).ShouldBe([3]);
    }

    /// <summary>
    /// Непубличная ветка целиком — нормальная конфигурация.
    /// </summary>
    [Fact]
    public void PrivateSubtreeIsNotViolation()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [2] },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false, [3] = false });

        Violations(tree).ShouldBeEmpty();
    }

    /// <summary>
    /// Группы — граф: достаточно одного публичного пути наверх.
    /// </summary>
    [Fact]
    public void SecondPublicPathIsEnough()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [Root], [4] = [2, 3] },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false });

        Violations(tree).ShouldBeEmpty();
    }

    /// <summary>
    /// Нарушение наследуется вглубь: под непубличной группой не спасает даже публичная цепочка.
    /// </summary>
    [Fact]
    public void ViolationPropagatesDownTheBranch()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [2], [4] = [3] },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false });

        Violations(tree).ShouldBe([3, 4], ignoreOrder: true);
    }

    [Fact]
    public void DeletedGroupIsNotViolation()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [2] },
            isActiveByGroup: new Dictionary<int, bool> { [3] = false },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false });

        Violations(tree).ShouldBeEmpty();
    }

    /// <summary>
    /// Спецгруппу с этой страницы не исправить: её публичность задаётся вариантом поля.
    /// </summary>
    [Fact]
    public void SpecialGroupIsNotViolation()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [Root], [3] = [2] },
            types: new Dictionary<int, CharacterGroupType> { [3] = CharacterGroupType.SpecialToField },
            isPublicByGroup: new Dictionary<int, bool> { [2] = false });

        Violations(tree).ShouldBeEmpty();
    }

    /// <summary>
    /// Цикл в графе групп не должен подвешивать проверку. Обход идёт сверху вниз от корня,
    /// поэтому отдельно висящий цикл просто недостижим — и его группы считаются нарушителями.
    /// </summary>
    [Fact]
    public void CycleDoesNotHangTheCheck()
    {
        var tree = MakeGroupTree(new Dictionary<int, int[]> { [2] = [3], [3] = [2] });

        Violations(tree).ShouldBe([2, 3], ignoreOrder: true);
    }
}
