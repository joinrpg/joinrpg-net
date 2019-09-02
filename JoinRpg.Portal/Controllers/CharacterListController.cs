using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Exporters;

namespace JoinRpg.Portal.Controllers
{
    [MasterAuthorize()]
  public class CharacterListController : ControllerGameBase
  {

    private IPlotRepository PlotRepository { get; }
    private IUriService UriService { get; }
        private IExportDataService ExportDataService { get; }

    [HttpGet]
    public Task<ActionResult> Active(int projectid, string export)
     => MasterCharacterList(projectid, claim => claim.IsActive, export, "Все персонажи");

    [HttpGet]
    public Task<ActionResult> Deleted(int projectId, string export)
      => MasterCharacterList(projectId, character => !character.IsActive, export, "Удаленные персонажи");


    [HttpGet]
    public Task<ActionResult> Problems(int projectid, string export)
      => MasterCharacterList(projectid,
        character => character.ApprovedClaimId != null && character.GetProblems().Any(), export,
        "Проблемные персонажи");

    [HttpGet]
    public async Task<ActionResult> ByUnAssignedField(int projectfieldid, int projectid, string export)
    {
      var field = await ProjectRepository.GetProjectField(projectid, projectfieldid);
      return await MasterCharacterList(projectid,
        character => character.HasProblemsForField(field) && character.IsActive, export, "Поле (непроставлено): " + field.FieldName);
    }

    private async Task<ActionResult> MasterCharacterList(int projectId, Func<Character, bool> predicate, string export, string title)
        {
            var characters = (await ProjectRepository.GetCharacters(projectId)).Where(predicate).ToList();

            var project = await GetProjectFromList(projectId, characters);

            var list = new CharacterListViewModel(CurrentUserId, title, characters, project);

            var exportType = ExportTypeNameParserHelper.ToExportType(export);

            if (exportType == null)
            {
                return View("Index", list);
            }

            return await Export(list, exportType);
        }

        public CharacterListController(
            IProjectRepository projectRepository,
            IProjectService projectService,
            IExportDataService exportDataService,
            IPlotRepository plotRepository,
            IUriService uriService,
            IUserRepository userRepository)
         : base(projectRepository, projectService, userRepository)
        {
            PlotRepository = plotRepository;
            UriService = uriService;
            ExportDataService = exportDataService;
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

      if (characterGroup == null) return NotFound();

      var plots = await PlotRepository.GetPlotsWithTargets(projectId);

      var list = new CharacterListByGroupViewModel(CurrentUserId,
        characters, characterGroup);

      var exportType = ExportTypeNameParserHelper.ToExportType(export);

      if (exportType == null)
      {
        return View("ByGroup", list);
      }

            return await Export(list, exportType);
    }

    [HttpGet]
    public async Task<ActionResult> ByAssignedField(int projectfieldid, int projectid, string export)
    {
      var field = await ProjectRepository.GetProjectField(projectid, projectfieldid);
      return await MasterCharacterList(projectid,
        character => character.GetFields().Single(f => f.Field.ProjectFieldId == projectfieldid).HasEditableValue && character.IsActive, export,
        "Поле (проставлено): " + field.FieldName);
    }

        [HttpGet]
        public Task<ActionResult> Vacant(int projectid, string export)
          => MasterCharacterList(projectid, character => character.ApprovedClaim == null && character.IsActive, export, "Свободные персонажи");

        [HttpGet]
        public Task<ActionResult> WithPlayers(int projectid, string export)
          => MasterCharacterList(projectid, character => character.ApprovedClaim != null && character.IsActive, export, "Занятые персонажи");

        private async Task<FileContentResult> Export(CharacterListViewModel list, ExportType? exportType)
        {
            var generator = ExportDataService.GetGenerator(
                exportType.Value,
                list.Items,
              new CharacterListItemViewModelExporter(list.Fields, UriService));

            return await ReturnExportResult(list.ProjectName + ": " + list.Title, generator);
        }

        private async Task<FileContentResult> ReturnExportResult(string fileName, IExportGenerator generator) =>
            File(await generator.Generate(), generator.ContentType,
                Path.ChangeExtension(fileName.ToSafeFileName(), generator.FileExtension));
  }


}
