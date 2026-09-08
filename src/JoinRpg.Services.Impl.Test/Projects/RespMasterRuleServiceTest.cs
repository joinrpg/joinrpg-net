using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Projects.Metadata;

namespace JoinRpg.Services.Impl.Test.Projects;

public class RespMasterRuleServiceTest : ProjectMetadataServiceTestBase
{
    private RespMasterRuleService CreateService(int? currentUserId = null, bool isAdmin = false)
        => new RespMasterRuleService(CreatePropsService(CreateCurrentUser(currentUserId, isAdmin)));

    private CharacterGroupIdentification GroupId => new(ProjectId, mock.Group.CharacterGroupId);

    [Fact]
    public async Task AddRuleShouldAppearInProjectInfo()
    {
        await CreateService().AddRule(ProjectId, mock.Group.CharacterGroupId, mock.Master.UserId);

        Result.Groups[GroupId].ResponsibleMasterId.ShouldBe(new UserIdentification(mock.Master.UserId));
        Result.ResponsibleMasterRules.ShouldHaveSingleItem().Id.ShouldBe(GroupId);
    }

    [Fact]
    public async Task RemoveRuleShouldDisappearFromProjectInfo()
    {
        var service = CreateService();
        await service.AddRule(ProjectId, mock.Group.CharacterGroupId, mock.Master.UserId);

        await service.RemoveRule(ProjectId, mock.Group.CharacterGroupId);

        Result.Groups[GroupId].ResponsibleMasterId.ShouldBeNull();
        Result.ResponsibleMasterRules.ShouldBeEmpty();
    }

    [Fact]
    public async Task RuleCouldBeSetOnSpecialGroup()
    {
        var specialGroup = mock.CreateCharacterGroup();
        specialGroup.IsSpecial = true;
        mock.ReInitProjectInfo();
        var specialGroupId = new CharacterGroupIdentification(ProjectId, specialGroup.CharacterGroupId);

        await CreateService().AddRule(ProjectId, specialGroup.CharacterGroupId, mock.Master.UserId);

        Result.Groups[specialGroupId].ResponsibleMasterId.ShouldBe(new UserIdentification(mock.Master.UserId));
    }

    [Fact]
    public async Task CantAssignMasterWithoutAccessToProject()
        => _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService().AddRule(ProjectId, mock.Group.CharacterGroupId, mock.Player.UserId));

    [Fact]
    public async Task CantChangeRulesWithoutManageClaimsPermission()
        => _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(currentUserId: mock.Player.UserId)
                .AddRule(ProjectId, mock.Group.CharacterGroupId, mock.Master.UserId));

    [Fact]
    public async Task CantChangeRulesInArchivedProject()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().AddRule(ProjectId, mock.Group.CharacterGroupId, mock.Master.UserId));
    }
}
