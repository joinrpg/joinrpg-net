using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Interfaces;
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
            var menuModel = new MasterMenuViewModel()
            {
                Permissions = acl.Permissions,
                CheckInModuleEnabled = projectInfo.ProjectCheckInSettings.CheckInModuleEnabled,
            };
            await SetCommonMenuParameters(menuModel, projectInfo);
            return View("MasterMenu", menuModel);
        }
        else
        {
            var project = await projectRepository.GetProjectAsync(currentProjectAccessor.ProjectId);
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
            var menuModel = new PlayerMenuViewModel()
            {
                Claims = claims,
                PlotPublished = projectInfo.PublishPlot,
            };
            await SetCommonMenuParameters(menuModel, projectInfo);
            return View("PlayerMenu", menuModel);
        }
    }

    private async Task SetCommonMenuParameters(MenuViewModelBase menuModel, ProjectInfo projectInfo)
    {
        menuModel.ProjectId = projectInfo.ProjectId;
        menuModel.ProjectName = projectInfo.ProjectName;
        menuModel.BigGroups = [..
            (await projectRepository.LoadDirectChildGroupHeaders(projectInfo.RootCharacterGroupId))
            .Select(dto => new CharacterGroupLinkSlimViewModel(dto))
            ];
        menuModel.ProjectStatus = projectInfo.ProjectStatus;
        menuModel.EnableAccommodation = projectInfo.AccomodationEnabled;
        menuModel.IsAdmin = currentUserAccessor.IsAdmin;
        menuModel.ShowSchedule = projectInfo.ProjectScheduleSettings.ScheduleEnabled;
    }
}
