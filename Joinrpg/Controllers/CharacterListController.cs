using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class CharacterListController : ControllerGameBase
  {
    private IPlotRepository PlotRepository { get; }

    [HttpGet, Authorize]
    public Task<ActionResult> Active(int projectid, string export)
     => MasterCharacterList(projectid, claim => claim.IsActive, "Index", export, "Все персонажи");

    [HttpGet, Authorize]
    public Task<ActionResult> Deleted(int projectId, string export)
      => MasterCharacterList(projectId, character => !character.IsActive, "Index", export, "Удаленные персонажи");

    [HttpGet, Authorize]
    public async Task<ActionResult> ByUnAssignedField(int projectfieldid, int projectid, string export)
    {
      var field = await ProjectRepository.GetProjectField(projectid, projectfieldid);
      return await MasterCharacterList(projectid,
        character => character.HasProblemsForField(field) && character.IsActive,
        "Index", export, "Поле: " + field.FieldName);
    }

    private async Task<ActionResult> MasterCharacterList(int projectId, Func<Character, bool> predicate,
      [AspMvcView] string viewName, string export, string title)
    {
      var characters = (await ProjectRepository.GetCharacters(projectId)).Where(predicate).ToList();

      var error = await AsMaster(characters, projectId);
      if (error != null) return error;

      var plots = await PlotRepository.GetPlotsWithTargets(projectId);

      var viewModel = new List<CharacterListItemViewModel>(characters.Count);
      foreach (var character in characters)
      {
        var plotElements =
          plots.SelectMany(p => p.Elements)
            .Where(
              p => p.TargetCharacters.Contains(character) || p.TargetGroups.Intersect(character.GetParentGroups()).Any());
        viewModel.Add(new CharacterListItemViewModel(character, CurrentUserId, character.GetProblems(), plotElements.ToArray()));
      }

      var exportType = GetExportTypeByName(export);

      if (exportType == null)
      {
        ViewBag.ClaimIds = viewModel.Select(c => c.ApprovedClaimId).WhereNotNull().ToArray();
        ViewBag.Title = title;
        return View(viewName, viewModel);
      }

      var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);

      return await ExportWithCustomFronend(viewModel, title, exportType.Value, project, new CharacterListItemViewModelExporter(project.ProjectFields));
    }

    public CharacterListController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService, IExportDataService exportDataService, IPlotRepository plotRepository) : base(userManager, projectRepository, projectService, exportDataService)
    {
      PlotRepository = plotRepository;
    }
  }
}