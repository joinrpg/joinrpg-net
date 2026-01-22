using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.Web.ProjectMasterTools.CaptainRules;

namespace JoinRpg.WebPortal.Managers.ProjectMasterTools.CaptainRules;

internal class CaptainRuleViewService(
    ICaptainRulesRepository repository,
    ICaptainRuleService service,
    IProjectRepository projectRepository,
    IUserRepository userRepository,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository
    ) : ICaptainRuleClient
{
    public async Task<CaptainRuleListViewModel> GetList(ProjectIdentification projectId)
    {
        var rules = await repository.GetCaptainRules(projectId);

        var groupIds = rules.Select(x => x.CharacterGroup).Distinct().ToList();
        var userIds = rules.Select(x => x.Player).Distinct().ToList();

        var groups = (await projectRepository.GetGroupHeaders(groupIds)).ToDictionary(x => x.CharacterGroupId);
        var users = (await userRepository.GetRequiredUserInfoHeaders(userIds)).ToDictionary(x => x.UserId);

        var ruleViewModels = rules.Select(rule =>
                new CaptainRuleViewModel(rule, groups[rule.CharacterGroup].Name, UserLinks.Create(users[rule.Player])))
            .ToList();

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        return new CaptainRuleListViewModel(
                Items: ruleViewModels,
                HasEditAccess: projectInfo.HasMasterAccess(currentUserAccessor.UserIdentification, Permission.CanManageClaims)
            );
    }

    async Task ICaptainRuleClient.Remove(CaptainAccessRule captainAccessRule)
        => await service.RemoveRule(captainAccessRule);
    async Task<CaptainRuleListViewModel> ICaptainRuleClient.AddOrChange(CaptainAccessRule captainAccessRule)
    {
        await service.AddOrChangeRule(captainAccessRule);
        return await GetList(captainAccessRule.ProjectId);
    }
}
