using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.Plot;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/plot/[action]")]
public class PlotListController(
    IProjectRepository projectRepository,
    IProjectService projectService,
    IPlotRepository plotRepository,
    IUriService uriService,
    IUserRepository userRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUser
    ) : ControllerGameBase(projectRepository,
    projectService,
    userRepository)
{
    [RequireMasterOrPublish]
    [HttpGet]
    public async Task<ActionResult> Index(ProjectIdentification projectId) => await PlotList(projectId, pf => true);

    [RequireMasterOrPublish]
    [HttpGet]
    public async Task<ActionResult> InWork(ProjectIdentification projectId) => await PlotList(projectId, pf => pf.InWork);

    [RequireMasterOrPublish]
    [HttpGet]
    public async Task<ActionResult> Ready(ProjectIdentification projectId) => await PlotList(projectId, pf => pf.Completed);

    [RequireMasterOrPublish]
    [HttpGet]
    public async Task<ActionResult> ByTag(int projectid, string tagname)
    {
        var allFolders = await plotRepository.GetPlotsByTag(projectid, tagname);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectid));
        return View("Index", new PlotFolderListViewModel(allFolders, currentUser, projectInfo));
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
        var characterGroups = group.GetChildrenGroupsRecursive().Union([group]).ToList();
        var characters = characterGroups.SelectMany(g => g.Characters).Distinct().Select(c => c.CharacterId).ToList();
        var characterGroupIds = characterGroups.Select(c => c.CharacterGroupId).ToList();
        var folders = await plotRepository.GetPlotsForTargets(projectId, characters, characterGroupIds);

        var groupNavigation = new CharacterGroupDetailsViewModel(group, CurrentUserIdOrDefault, GroupNavigationPage.Plots);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));

        return View("ForGroup", new PlotFolderListViewModelForGroup(folders, currentUser, groupNavigation, projectInfo));
    }

    [RequireMasterOrPublish]
    [HttpGet]
    public async Task<ActionResult> FlatList(int projectId)
    {
        var folders = (await plotRepository.GetPlotsWithTargetAndText(projectId)).ToList();
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        return View(
            new PlotFolderFullListViewModel(
                folders,
                currentUser,
                uriService,
                projectInfo));
    }

    [RequireMasterOrPublish]
    [HttpGet]
    public async Task<ActionResult> FlatListUnready(int projectId)
    {
        var folders = (await plotRepository.GetPlotsWithTargetAndText(projectId)).ToList();
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        return View("FlatList",
            new PlotFolderFullListViewModel(folders, currentUser, uriService, projectInfo, true));
    }

    private async Task<ActionResult> PlotList(ProjectIdentification projectId, Func<PlotFolder, bool> predicate)
    {
        var allFolders = await plotRepository.GetPlots(projectId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        if (allFolders.Count == 0)
        {
            return View("Index", new PlotFolderListViewModel([], currentUser, projectInfo));
        }

        var order = allFolders.First().GetPlotElementsContainer();
        var folders = allFolders.Where(predicate).ToList(); //Sadly, we have to do this, as we can't query using complex properties
        return View("Index", new PlotFolderListViewModel(folders, currentUser, projectInfo));
    }
}
