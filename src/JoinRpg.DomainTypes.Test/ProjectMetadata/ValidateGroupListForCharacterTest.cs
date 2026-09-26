using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;
using static JoinRpg.DomainTypes.Test.ProjectInfoFixture;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

/// <summary>
/// Правило «какие группы достанутся персонажу» живёт в <see cref="ProjectGroupTree"/>: ему нужно
/// только дерево групп, ни контекста операции, ни EF-графа.
/// </summary>
public class ValidateGroupListForCharacterTest
{
    private static ProjectGroupTree TreeWithRegularGroup()
        => MakeGroupTree(new Dictionary<int, int[]> { [2] = [1] });

    private static ProjectGroupTree TreeWithoutLiveRegularGroups()
        => MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [1] },
            isActiveByGroup: new Dictionary<int, bool> { [2] = false });

    [Fact]
    public void ShouldValidateGroupListWhenProjectAllowsToSetGroups()
    {
        var tree = TreeWithRegularGroup();
        tree.AllowToSetGroups.ShouldBeTrue();

        tree.ValidateGroupListForCharacter([GroupId(2)]).ShouldBe([GroupId(2)]);
    }

    [Fact]
    public void ShouldRejectGroupsOfAnotherProject()
    {
        var alienGroupId = new CharacterGroupIdentification(new ProjectIdentification(ProjectId.Value + 100), 2);

        _ = Should.Throw<ArgumentException>(
            () => TreeWithRegularGroup().ValidateGroupListForCharacter([alienGroupId]));
    }

    [Fact]
    public void ShouldRejectSpecialGroupWhenProjectAllowsToSetGroups()
    {
        var tree = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [1], [3] = [1] },
            types: new Dictionary<int, CharacterGroupType> { [3] = CharacterGroupType.SpecialToField });

        _ = Should.Throw<SpecialCharacterGroupNotAllowedException>(
            () => tree.ValidateGroupListForCharacter([GroupId(3)]));
    }

    /// <summary>
    /// Пустой список — ошибка валидации, а не молчаливый переход к корневой группе. Тип исключения —
    /// доменный <c>JoinValidationException</c>, а не EF-шный: сервисный слой развязан с ним в #4918.
    /// </summary>
    [Fact]
    public void ShouldRejectEmptyGroupListWhenProjectAllowsToSetGroups()
        => Should.Throw<JoinValidationException>(
            () => TreeWithRegularGroup().ValidateGroupListForCharacter([]));

    [Fact]
    public void ShouldReturnRootGroupWhenProjectForbidsToSetGroups()
    {
        // Кроме корневой, живых обычных групп в проекте не осталось — выбирать мастеру нечего.
        var tree = TreeWithoutLiveRegularGroups();
        tree.AllowToSetGroups.ShouldBeFalse();

        // Переданный список игнорируется целиком, в том числе и не проверяется.
        tree.ValidateGroupListForCharacter([GroupId(2)]).ShouldBe([RootGroupId]);
    }

    [Fact]
    public void ShouldReturnRootGroupForEmptyListWhenProjectForbidsToSetGroups()
        => TreeWithoutLiveRegularGroups().ValidateGroupListForCharacter([]).ShouldBe([RootGroupId]);
}
