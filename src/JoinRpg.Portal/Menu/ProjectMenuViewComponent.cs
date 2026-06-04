using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebPortal.Managers.Claims;
using JoinRpg.WebPortal.Managers.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Menu;

public class ProjectMenuViewComponent(
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentProjectAccessor currentProjectAccessor,
    IClaimsRepository claimsRepository,
    ICaptainRulesRepository captainRulesRepository
    ) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(currentProjectAccessor.ProjectId);

        if (currentUserAccessor.UserIdentificationOrDefault is not UserIdentification userId)
        {
            return GenerateAnonMenu(projectInfo);
        }

        var acl = projectInfo.Masters.FirstOrDefault(a => a.UserId == userId);

        if (acl != null)
        {
            return GenerateMasterMenu(projectInfo, acl);
        }
        else
        {
            return await GeneratePlayerMenu(projectInfo, userId);
        }
    }

    private IViewComponentResult GenerateAnonMenu(ProjectInfo projectInfo)
    {
        var bigGroups = LoadBigGroups(projectInfo).Where(g => g.IsPublic).ToArray();
        var menuModel = new PlayerMenuViewModel(projectInfo, currentUserAccessor, bigGroups, [], []);
        return View("PlayerMenu", menuModel);
    }

    private async Task<IViewComponentResult> GeneratePlayerMenu(ProjectInfo projectInfo, UserIdentification userId)
    {
        var claims = (await claimsRepository.GetClaimsHeadersForPlayer(projectInfo.ProjectId, ClaimStatusSpec.Active, userId)).ToClaimViewModels().ToList();
        var captainAccessRules = await captainRulesRepository.GetCaptainRules(projectInfo.ProjectId, userId);
        var bigGroups = LoadBigGroups(projectInfo).Where(g => g.IsPublic).ToArray();
        var menuModel = new PlayerMenuViewModel(projectInfo, currentUserAccessor, bigGroups, claims, captainAccessRules);
        return View("PlayerMenu", menuModel);
    }

    private IViewComponentResult GenerateMasterMenu(ProjectInfo projectInfo, ProjectMasterInfo acl)
    {
        var bigGroups = LoadBigGroups(projectInfo).ToArray();
        var menuModel = new MasterMenuViewModel(projectInfo, currentUserAccessor, bigGroups, acl.Permissions);
        return View("MasterMenu", menuModel);
    }

    private IEnumerable<CharacterGroupLinkSlimViewModel> LoadBigGroups(ProjectInfo projectInfo)
    {
        return projectInfo.GetDirectChildGroups(projectInfo.RootCharacterGroupId)
            .Select(dto => new CharacterGroupLinkSlimViewModel(dto.Id, dto.Name, dto.IsPublic, dto.IsActive));
    }
}
