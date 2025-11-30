using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebPortal.Managers.Claims;
using JoinRpg.WebPortal.Managers.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Menu;

public class ProjectMenuViewComponent(
    ICurrentUserAccessor currentUserAccessor,
    IProjectRepository projectRepository,
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
            return await GenerateAnonMenu(projectInfo);
        }

        var acl = projectInfo.Masters.FirstOrDefault(a => a.UserId == userId);

        if (acl != null)
        {
            return await GenerateMasterMenu(projectInfo, acl);
        }
        else
        {
            return await GeneratePlayerMenu(projectInfo, userId);
        }
    }

    private async Task<IViewComponentResult> GenerateAnonMenu(ProjectInfo projectInfo)
    {
        var bigGroups = (await LoadBigGroups(projectInfo)).Where(g => g.IsPublic).ToArray();
        var menuModel = new PlayerMenuViewModel(projectInfo, currentUserAccessor, bigGroups, [], []);
        return View("PlayerMenu", menuModel);
    }

    private async Task<IViewComponentResult> GeneratePlayerMenu(ProjectInfo projectInfo, UserIdentification userId)
    {
        var claims = (await claimsRepository.GetClaimsHeadersForPlayer(projectInfo.ProjectId, ClaimStatusSpec.Active, userId)).ToClaimViewModels().ToList();
        var captainAccessRules = await captainRulesRepository.GetCaptainRules(projectInfo.ProjectId, userId);
        var bigGroups = (await LoadBigGroups(projectInfo)).Where(g => g.IsPublic).ToArray();
        var menuModel = new PlayerMenuViewModel(projectInfo, currentUserAccessor, bigGroups, claims, captainAccessRules);
        return View("PlayerMenu", menuModel);
    }

    private async Task<IViewComponentResult> GenerateMasterMenu(ProjectInfo projectInfo, ProjectMasterInfo acl)
    {
        var bigGroups = (await LoadBigGroups(projectInfo)).ToArray();
        var menuModel = new MasterMenuViewModel(projectInfo, currentUserAccessor, bigGroups, acl.Permissions);
        return View("MasterMenu", menuModel);
    }

    private async Task<IEnumerable<CharacterGroupLinkSlimViewModel>> LoadBigGroups(ProjectInfo projectInfo)
    {
        return (await projectRepository.LoadDirectChildGroupHeaders(projectInfo.RootCharacterGroupId))
            .Select(dto => new CharacterGroupLinkSlimViewModel(dto.CharacterGroupId, dto.Name, dto.IsPublic, dto.IsActive));
    }
}
