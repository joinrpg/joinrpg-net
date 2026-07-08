using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Helpers.Test;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.CharacterGroups.Test;

public class JsonRoundTripTests
{
    [Fact]
    public void ProjectRoleGridGroupHeaderRowViewModelShouldRoundTripThroughJson()
    {
        var instance = new ProjectRoleGridGroupHeaderRowViewModel(new CharacterGroupLinkSlimViewModel(new CharacterGroupIdentification(1, 1), "GroupName", IsPublic: false, IsActive: false), "<p>Описание группы</p>", CharacterGroupType.Regular);
        instance.ShouldRoundTripThroughJson();
    }
}
