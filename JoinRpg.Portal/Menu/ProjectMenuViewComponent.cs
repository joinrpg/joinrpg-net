using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;
using JoinRpg.WebPortal.Managers.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Menu
{
    public class ProjectMenuViewComponent: ViewComponent
    {
        public ProjectMenuViewComponent(ICurrentUserAccessor currentUserAccessor, IProjectRepository projectRepository, ICurrentProjectAccessor currentProjectAccessor)
        {
            CurrentUserAccessor = currentUserAccessor;
            ProjectRepository = projectRepository;
            CurrentProjectAccessor = currentProjectAccessor;
        }

        private ICurrentUserAccessor CurrentUserAccessor { get; }
        private IProjectRepository ProjectRepository { get; }
        private ICurrentProjectAccessor CurrentProjectAccessor { get; }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var project = await ProjectRepository.GetProjectAsync(CurrentProjectAccessor.ProjectId);

            var acl = project.ProjectAcls.FirstOrDefault(a => a.UserId == CurrentUserAccessor.UserIdOrDefault);

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
                    Claims = project.Claims.OfUserActive(CurrentUserAccessor.UserIdOrDefault).Select(c => new ClaimShortListItemViewModel(c)).ToArray(),
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
                    cg => !cg.IsSpecial && cg.IsActive && cg.IsVisible(CurrentUserAccessor.UserIdOrDefault))
                .Select(cg => new CharacterGroupLinkViewModel(cg)).ToList();
            menuModel.IsAcceptingClaims = project.IsAcceptingClaims;
            menuModel.IsActive = project.Active;
            menuModel.RootGroupId = project.RootGroup.CharacterGroupId;
            menuModel.EnableAccommodation = project.Details.EnableAccommodation;
            menuModel.IsAdmin = CurrentUserAccessor.IsAdmin;
        }
    }
}
