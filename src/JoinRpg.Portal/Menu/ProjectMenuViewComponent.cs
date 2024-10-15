using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebPortal.Managers.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Menu;

public class ProjectMenuViewComponent(
    ICurrentUserAccessor currentUserAccessor,
    IProjectRepository projectRepository,
    ICurrentProjectAccessor currentProjectAccessor) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var project = await projectRepository.GetProjectAsync(currentProjectAccessor.ProjectId);

        var acl = project.ProjectAcls.FirstOrDefault(a => a.UserId == currentUserAccessor.UserIdOrDefault);

        if (acl != null)
        {
            var menuModel = new MasterMenuViewModel()
            {
                AccessToProject = acl,
                CheckInModuleEnabled = project.Details.EnableCheckInModule,
            };
            SetCommonMenuParameters(menuModel, project);
            return View("MasterMenu", menuModel);
        }
        else
        {
            var menuModel = new PlayerMenuViewModel()
            {
                Claims = project.Claims.OfUserActive(currentUserAccessor.UserIdOrDefault).Select(c => new ClaimShortListItemViewModel(c)).ToArray(),
                PlotPublished = project.Details.PublishPlot,
            };
            SetCommonMenuParameters(menuModel, project);
            return View("PlayerMenu", menuModel);
        }
    }

    private void SetCommonMenuParameters(MenuViewModelBase menuModel, Project project)
    {
        menuModel.ProjectId = project.ProjectId;
        menuModel.ProjectName = project.ProjectName;
        //TODO[GroupsLoad]. If we not loaded groups already, that's slow
        menuModel.BigGroups = project.RootGroup.ChildGroups.Where(
                cg => !cg.IsSpecial && cg.IsActive && cg.IsVisible(currentUserAccessor.UserIdOrDefault))
            .Select(cg => new CharacterGroupLinkSlimViewModel(new(cg.ProjectId), cg.CharacterGroupId, cg.CharacterGroupName, cg.IsPublic, cg.IsActive)).ToList();
        menuModel.IsAcceptingClaims = project.IsAcceptingClaims;
        menuModel.IsActive = project.Active;
        menuModel.EnableAccommodation = project.Details.EnableAccommodation;
        menuModel.IsAdmin = currentUserAccessor.IsAdmin;
        menuModel.ShowSchedule = project.Details.ScheduleEnabled;
    }
}
