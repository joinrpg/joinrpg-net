using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;

namespace JoinRpg.WebPortal.Managers.ProjectMasterTools.ResponsibleMasterRules;

internal class ResponsibleMasterRuleViewService(
    IProjectMetadataRepository projectMetadataRepository,
    IRespMasterRuleService service,
    ICurrentUserAccessor currentUserAccessor) : IResponsibleMasterRuleClient
{
    public async Task<ResponsibleMasterRuleListViewModel> GetResponsibleMasterRuleList(ProjectIdentification projectId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var sortedGroups = projectInfo.ResponsibleMasterRules
            .Select(g => g.ToRespRuleViewModel(projectInfo.GetMasterById(g.ResponsibleMasterId!)))
            .ToList();

        var defaultMaster = projectInfo.GetDefaultResponsibleMaster();
        sortedGroups.Add(
            ResponsibleMasterRuleViewModel.CreateSpecial(
                UserLinks.Create(defaultMaster.UserInfo)
            ));

        return new ResponsibleMasterRuleListViewModel(
                Items: sortedGroups,
                HasEditAccess: projectInfo.HasMasterAccess(
                    currentUserAccessor,
                    Permission.CanManageClaims
                    )
            );
    }

    public async Task<ResponsibleMasterRuleListViewModel> AddResponsibleMasterRule(ProjectIdentification projectIdentification, int groupId, int masterId)
    {
        await service.AddRule(projectIdentification, groupId, masterId);
        return await GetResponsibleMasterRuleList(projectIdentification);
    }

    public async Task RemoveResponsibleMasterRule(ProjectIdentification projectId, int ruleId)
        => await service.RemoveRule(projectId, ruleId);
}
