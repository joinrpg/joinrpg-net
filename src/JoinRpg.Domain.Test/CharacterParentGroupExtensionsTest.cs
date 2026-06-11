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
}
