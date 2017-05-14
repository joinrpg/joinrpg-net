using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Exporters;

namespace JoinRpg.Web.Controllers
{
  [MasterAuthorize()]
  public class CharacterListController : ControllerGameBase
  {

    private IPlotRepository PlotRepository { get; }
    private IUriService UriService { get; }

    [HttpGet]
    public Task<ActionResult> Active(int projectid, string export)
     => MasterCharacterList(projectid, claim => claim.IsActive, export, "Все персонажи");

    [HttpGet, AllowAnonymous]
    public async Task<ActionResult> ActiveToken(int projectid, string token)
    {
      var characters = (await ProjectRepository.GetCharacters(projectid)).Where(claim => claim.IsActive).ToList();

      var project = await ProjectRepository.GetProjectWithFinances(projectid);

      var guid = new Guid(token.FromHexString());

      var acl = project.ProjectAcls.SingleOrDefault(a => a.Token == guid);

      if (acl == null)
      {
        return Content("Unauthorized");
      }

      var plots = await PlotRepository.GetPlotsWithTargets(projectid);

      var list = new CharacterListViewModel(acl.UserId , "Все персонажи", characters, plots, project, vm => true);

      return await ExportWithCustomFronend(list.Items, list.Title, ExportType.Csv,
        new CharacterListItemViewModelExporter(list.Fields, UriService), list.ProjectName);
    }

    [HttpGet]
    public Task<ActionResult> Deleted(int projectId, string export)
      => MasterCharacterList(projectId, character => !character.IsActive, export, "Удаленные персонажи");


    [HttpGet]
    public Task<ActionResult> Problems(int projectid, string export)
     => MasterCharacterList(projectid, claim => claim.GetProblems().Any(), export, "Проблемные персонажи");

    [HttpGet]
    public Task<ActionResult> FreeCharactersWithPlot(int projectid, string export)
     => MasterCharacterList(projectid, character => character.ApprovedClaim == null, export, "Свободные персонажи с сюжетом", vm => vm.IndAllPlotsCount > 0);


    [HttpGet]
    public async Task<ActionResult> ByUnAssignedField(int projectfieldid, int projectid, string export)
    {
      var field = await ProjectRepository.GetProjectField(projectid, projectfieldid);
      return await MasterCharacterList(projectid,
        character => character.HasProblemsForField(field) && character.IsActive, export, "Поле (непроставлено): " + field.FieldName);
    }

    private Task<ActionResult> MasterCharacterList(int projectId, Func<Character, bool> predicate, string export,
      string title)
    {
      return MasterCharacterList(projectId, predicate, export, title, vm => true);
    }

    private async Task<ActionResult> MasterCharacterList(int projectId, Func<Character, bool> predicate, string export, string title, Func<CharacterListItemViewModel, bool> viewModelPredicate)
    {
      var characters = (await ProjectRepository.GetCharacters(projectId)).Where(predicate).ToList();

      var plots = await PlotRepository.GetPlotsWithTargets(projectId);
      var project = await GetProjectFromList(projectId, characters);

      var list = new CharacterListViewModel(CurrentUserId, title, characters, plots, project, viewModelPredicate);

      var exportType = GetExportTypeByName(export);

      if (exportType == null)
      {
        return View("Index", list);
      }

      return await ExportWithCustomFronend(list.Items, list.Title, exportType.Value, new CharacterListItemViewModelExporter(list.Fields, UriService), list.ProjectName);
    }

    public CharacterListController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService, IExportDataService exportDataService, IPlotRepository plotRepository, IUriService uriService) : base(userManager, projectRepository, projectService, exportDataService)
    {
      PlotRepository = plotRepository;
      UriService = uriService;
    }


    private async Task<int[]> GetChildrenGroupIds(int projectId, int characterGroupId)
    {
      var groups = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
      return groups.GetChildrenGroups().Select(g => g.CharacterGroupId).Union(characterGroupId).ToArray();
    }

    public async Task<ActionResult> ByGroup(int projectId, int characterGroupId, string export)
    {
      var characterGroup = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
      var groupIds = await GetChildrenGroupIds(projectId, characterGroupId);
      var characters =
        (await ProjectRepository.GetCharacterByGroups(projectId, groupIds)).Where(ch => ch.IsActive).ToList();

      if (characterGroup == null) return HttpNotFound();

      var plots = await PlotRepository.GetPlotsWithTargets(projectId);

      var list = new CharacterListByGroupViewModel(CurrentUserId,
        characters, plots, characterGroup);

      var exportType = GetExportTypeByName(export);

      if (exportType == null)
      {
        return View("ByGroup", list);
      }

      return await ExportWithCustomFronend(list.Items, list.Title, exportType.Value,
        new CharacterListItemViewModelExporter(list.Fields, UriService), list.ProjectName);
    }

    [HttpGet]
    public async Task<ActionResult> ByAssignedField(int projectfieldid, int projectid, string export)
    {
      var field = await ProjectRepository.GetProjectField(projectid, projectfieldid);
      return await MasterCharacterList(projectid,
        character => character.GetFields().Single(f => f.Field.ProjectFieldId == projectfieldid).HasEditableValue && character.IsActive, export,
        "Поле (проставлено): " + field.FieldName);
    }
  }
}