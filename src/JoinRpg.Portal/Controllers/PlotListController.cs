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

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/plot/[action]")]
public class PlotListController(
    IProjectRepository projectRepository,
    IProjectService projectService,
    IPlotRepository plotRepository,
    IUriService uriService,
    IUserRepository userRepository,
    IProjectMetadataRepository projectMetadataRepository) : ControllerGameBase(projectRepository,
    projectService,
    userRepository)
{
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
        var allFolders = await plotRepository.GetPlotsByTag(projectid, tagname);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectid));
#pragma warning disable CS0612 // Type or member is obsolete
        var project = await GetProjectFromList(projectid, allFolders);
#pragma warning restore CS0612 // Type or member is obsolete
        return View("Index", new PlotFolderListViewModel(allFolders, project, CurrentUserIdOrDefault, projectInfo));
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
        var project = group.Project;

        var groupNavigation = new CharacterGroupDetailsViewModel(group, CurrentUserIdOrDefault, GroupNavigationPage.Plots);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));

        return View("ForGroup", new PlotFolderListViewModelForGroup(folders, project, CurrentUserIdOrDefault, groupNavigation, projectInfo));
    }

    [RequireMasterOrPublish]
    [HttpGet]
    public async Task<ActionResult> FlatList(int projectId)
    {
        var folders = (await plotRepository.GetPlotsWithTargetAndText(projectId)).ToList();
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));
#pragma warning disable CS0612 // Type or member is obsolete
        var project = await GetProjectFromList(projectId, folders);
#pragma warning restore CS0612 // Type or member is obsolete
        return View(
            new PlotFolderFullListViewModel(
                folders,
                project,
                CurrentUserIdOrDefault,
                uriService, projectInfo));
    }

    [RequireMasterOrPublish]
    [HttpGet]
    public async Task<ActionResult> FlatListUnready(int projectId)
    {
        var folders = (await plotRepository.GetPlotsWithTargetAndText(projectId)).ToList();
#pragma warning disable CS0612 // Type or member is obsolete
        var project = await GetProjectFromList(projectId, folders);
#pragma warning restore CS0612 // Type or member is obsolete
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        return View("FlatList",
            new PlotFolderFullListViewModel(folders, project, CurrentUserIdOrDefault, uriService, projectInfo, true));
    }

    private async Task<ActionResult> PlotList(int projectId, Func<PlotFolder, bool> predicate)
    {
        var allFolders = await plotRepository.GetPlots(projectId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        var folders = allFolders.Where(predicate).ToList(); //Sadly, we have to do this, as we can't query using complex properties
#pragma warning disable CS0612 // Type or member is obsolete
        var project = await GetProjectFromList(projectId, folders);
#pragma warning restore CS0612 // Type or member is obsolete
        return View("Index", new PlotFolderListViewModel(folders, project, CurrentUserIdOrDefault, projectInfo));
    }
}
