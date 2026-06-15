using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.Web.ProjectMasterTools.CaptainRules;

namespace JoinRpg.WebPortal.Managers.ProjectMasterTools.CaptainRules;

internal class CaptainRuleViewService(
    ICaptainRulesRepository repository,
    ICaptainRuleService service,
    IUserRepository userRepository,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository
    ) : ICaptainRuleClient
{
    public async Task<CaptainRuleListViewModel> GetList(ProjectIdentification projectId)
    {
        var rules = await repository.GetCaptainRules(projectId);

        var userIds = rules.Select(x => x.Player).Distinct().ToList();
        var users = (await userRepository.GetRequiredUserInfoHeaders(userIds)).ToDictionary(x => x.UserId);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var ruleViewModels = rules.Select(rule =>
                new CaptainRuleViewModel(rule, projectInfo.Groups[rule.CharacterGroup].Name, UserLinks.Create(users[rule.Player])))
            .ToList();

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
