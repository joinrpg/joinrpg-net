using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
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
        return View("Index", PlotFolderListViewModelBuilder.ToPlotFolderListViewModel(allFolders, currentUser, projectInfo, "Сюжеты по тегу «" + tagname + "»"));
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

        var list = PlotFolderListViewModelBuilder.ToPlotFolderListViewModel(folders, currentUser, projectInfo, "Сюжеты группы «" + group.CharacterGroupName + "»");

        return View("ForGroup", new PlotFolderListViewModelForGroup(list, groupNavigation));
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
                projectInfo));
    }

    [RequireMasterOrPublish]
    [HttpGet]
    public async Task<ActionResult> FlatListUnready(int projectId)
    {
        var folders = (await plotRepository.GetPlotsWithTargetAndText(projectId)).ToList();
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        return View("FlatList",
            new PlotFolderFullListViewModel(folders, currentUser, projectInfo, true));
    }

    private async Task<ActionResult> PlotList(ProjectIdentification projectId, Func<PlotFolder, bool> predicate)
    {
        var allFolders = await plotRepository.GetPlots(projectId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var folders = allFolders.Where(predicate).ToList(); // TODO перенести эту фильтрацию на фронт
        return View("Index", PlotFolderListViewModelBuilder.ToPlotFolderListViewModel(folders, currentUser, projectInfo, "Сюжеты"));
    }
}
