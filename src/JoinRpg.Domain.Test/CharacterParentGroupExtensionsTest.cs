using JoinRpg.DataModel.Mocks;

namespace JoinRpg.Domain.Test;

public class CharacterParentGroupExtensionsTest
{
    [Fact]
    public void GetDirectNonSpecialGroupIds_ReturnsOnlyDirectGroups_NotAllAncestors()
    {
        var mock = new MockedProject();
        var rootGroup = mock.Project.CharacterGroups.Single(g => g.IsRoot);

        var midGroup = mock.CreateCharacterGroup();
        midGroup.ParentCharacterGroupIds = [rootGroup.CharacterGroupId];

        mock.Character.ParentCharacterGroupIds = [midGroup.CharacterGroupId];
        mock.ReInitProjectInfo();

        var result = mock.Character.GetDirectNonSpecialGroupIds(mock.ProjectInfo).ToList();

        result.ShouldHaveSingleItem();
        result[0].CharacterGroupId.ShouldBe(midGroup.CharacterGroupId);
    }

    [Fact]
    public void GetParentGroupIdsToTop_ByProjectInfo_ReturnsAllAncestorsIncludingInactive()
    {
        var mock = new MockedProject();
        var rootGroup = mock.Project.CharacterGroups.Single(g => g.IsRoot);

        var midGroup = mock.CreateCharacterGroup();
        midGroup.ParentCharacterGroupIds = [rootGroup.CharacterGroupId];

        var leafGroup = mock.CreateCharacterGroup();
        leafGroup.ParentCharacterGroupIds = [midGroup.CharacterGroupId];

        var inactiveGroup = mock.CreateCharacterGroup();
        inactiveGroup.ParentCharacterGroupIds = [rootGroup.CharacterGroupId];
        inactiveGroup.IsActive = false;

        // Две ветки сразу: так проверяется и объединение, и дедупликация общего предка.
        mock.Character.ParentCharacterGroupIds = [leafGroup.CharacterGroupId, inactiveGroup.CharacterGroupId];
        mock.ReInitProjectInfo();

        mock.Character.GetParentGroupIdsToTop(mock.ProjectInfo)
            .Select(g => g.CharacterGroupId)
            .ShouldBe(
                [rootGroup.CharacterGroupId, midGroup.CharacterGroupId, leafGroup.CharacterGroupId, inactiveGroup.CharacterGroupId],
                ignoreOrder: true);
    }
}
