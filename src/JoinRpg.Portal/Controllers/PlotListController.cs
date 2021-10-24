using System;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.Plot;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    [Route("{projectId}/plot/[action]")]
    public class PlotListController : ControllerGameBase
    {
        private readonly IPlotRepository _plotRepository;
        private IUriService UriService { get; }


        public PlotListController(
            IProjectRepository projectRepository,
            IProjectService projectService,
            IPlotRepository plotRepository,
            IUriService uriService,
            IUserRepository userRepository) : base(projectRepository,
            projectService,
            userRepository)
        {
            _plotRepository = plotRepository;
            UriService = uriService;
        }

        [RequireMasterOrPublish]
        [HttpGet]
        public async Task<ActionResult> Index(int projectId) => await PlotList(projectId, pf => true);

        [RequireMasterOrPublish]
        [HttpGet]
        public async Task<ActionResult> InWork(int projectId) => await PlotList(projectId, pf => pf.InWork);

        [RequireMasterOrPublish]
        [HttpGet]
        public async Task<ActionResult> Ready(int projectId) => await PlotList(projectId, pf => pf.Completed);

        [RequireMasterOrPublish]
        [HttpGet]
        public async Task<ActionResult> ByTag(int projectid, string tagname)
        {
            var allFolders = await _plotRepository.GetPlotsByTag(projectid, tagname);
            var project = await GetProjectFromList(projectid, allFolders);
            return View("Index", new PlotFolderListViewModel(allFolders, project, CurrentUserIdOrDefault));
        }


        [HttpGet("~/{ProjectId}/roles/{CharacterGroupId}/plots")]
        [RequireMasterOrPublish]
        public async Task<ActionResult> ForGroup(int projectId, int characterGroupId)
        {
            var group = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
            if (group == null)
            {
                return NotFound();
            }

            //TODO slow 
            var characterGroups = group.GetChildrenGroups().Union(new[] { group }).ToList();
            var characters = characterGroups.SelectMany(g => g.Characters).Distinct().Select(c => c.CharacterId).ToList();
            var characterGroupIds = characterGroups.Select(c => c.CharacterGroupId).ToList();
            var folders = await _plotRepository.GetPlotsForTargets(projectId, characters, characterGroupIds);
            var project = group.Project;

            var groupNavigation = new CharacterGroupDetailsViewModel(group, CurrentUserIdOrDefault, GroupNavigationPage.Plots);

            return View("ForGroup", new PlotFolderListViewModelForGroup(folders, project, CurrentUserIdOrDefault, groupNavigation));
        }

        [RequireMasterOrPublish]
        [HttpGet]
        public async Task<ActionResult> FlatList(int projectId)
        {
            var folders = (await _plotRepository.GetPlotsWithTargetAndText(projectId)).ToList();
            var project = await GetProjectFromList(projectId, folders);
            return View(
                new PlotFolderFullListViewModel(
                    folders,
                    project,
                    CurrentUserIdOrDefault,
                    UriService));
        }

        [RequireMasterOrPublish]
        [HttpGet]
        public async Task<ActionResult> FlatListUnready(int projectId)
        {
            var folders = (await _plotRepository.GetPlotsWithTargetAndText(projectId)).ToList();
            var project = await GetProjectFromList(projectId, folders);
            return View("FlatList",
                new PlotFolderFullListViewModel(folders, project, CurrentUserIdOrDefault, UriService, true));
        }

        private async Task<ActionResult> PlotList(int projectId, Func<PlotFolder, bool> predicate)
        {
            var allFolders = await _plotRepository.GetPlots(projectId);
            var folders = allFolders.Where(predicate).ToList(); //Sadly, we have to do this, as we can't query using complex properties
            var project = await GetProjectFromList(projectId, folders);
            return View("Index", new PlotFolderListViewModel(folders, project, CurrentUserIdOrDefault));
        }
    }
}
