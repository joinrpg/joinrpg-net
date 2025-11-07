using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebPortal.Managers.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Menu;

public class ProjectMenuViewComponent(
    ICurrentUserAccessor currentUserAccessor,
    IProjectRepository projectRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentProjectAccessor currentProjectAccessor,
    IClaimsRepository claimsRepository
    ) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(currentProjectAccessor.ProjectId);

        var acl = projectInfo.Masters.FirstOrDefault(a => a.UserId == currentUserAccessor.UserIdOrDefault);

        if (acl != null)
        {
            return await GenerateMasterMenu(projectInfo, acl);
        }
        else
        {
            return await GeneratePlayerMenu(projectInfo);
        }
    }

    private async Task<IViewComponentResult> GeneratePlayerMenu(ProjectInfo projectInfo)
    {
        IReadOnlyCollection<ClaimShortListItemViewModel> claims;
        if (currentUserAccessor.UserIdOrDefault is int userId)
        {
            claims = [..
                    (await claimsRepository.GetClaimsHeadersForPlayer(projectInfo.ProjectId, ClaimStatusSpec.Active, userId))
                    .Select(c => new ClaimShortListItemViewModel(c))
                ];
        }
        else
        {
            claims = [];
        }
        var bigGroups = (await LoadBigGroups(projectInfo)).Where(g => g.IsPublic).ToArray();
        var menuModel = new PlayerMenuViewModel(projectInfo, currentUserAccessor, bigGroups)
        {
            Claims = claims,
        };
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
            .Select(dto => new CharacterGroupLinkSlimViewModel(dto));
    }
}
