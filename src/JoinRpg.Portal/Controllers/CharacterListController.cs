using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Helpers;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Exporters;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[MasterAuthorize()]
[Route("{projectId}/characters/[action]")]
public class CharacterListController(
    IProjectRepository projectRepository,
    IProjectService projectService,
    IExportDataService exportDataService,
    IPlotRepository plotRepository,
    IUriService uriService,
    IUserRepository userRepository,
    IProjectMetadataRepository projectMetadataRepository,
    IProblemValidator<Character> problemValidator,
    ICharacterRepository characterRepository
    ) : ControllerGameBase(projectRepository, projectService, userRepository)
{
    [HttpGet]
    public Task<ActionResult> Active(ProjectIdentification projectid, string export)
     => MasterCharacterList(projectid, (character, projectInfo) => character.IsActive, export, "Все персонажи");

    [HttpGet]
    public Task<ActionResult> Deleted(ProjectIdentification projectId, string export)
      => MasterCharacterList(projectId, (character, projectInfo) => !character.IsActive, export, "Удаленные персонажи");


    [HttpGet]
    public Task<ActionResult> Problems(ProjectIdentification projectid, string export)
      => MasterCharacterList(projectid,
        (character, projectInfo) => character.IsActive && problemValidator.Validate(character, projectInfo).Any(), export,
        "Проблемные персонажи");

    [HttpGet]
    public async Task<ActionResult> ByUnAssignedField(int projectfieldId, ProjectIdentification projectId, string export)
    {
        var pi = await projectMetadataRepository.GetProjectMetadata(projectId);
        var field = pi.GetFieldById(new ProjectFieldIdentification(projectId, projectfieldId));

        return await MasterCharacterList(projectId,
          (character, projectInfo) => character.IsActive && problemValidator.ValidateFieldOnly(character, projectInfo, field.Id).Any(),
          export,
          "Поле (непроставлено): " + field.Name);
    }

    private async Task<ActionResult> MasterCharacterList(ProjectIdentification projectId, Func<Character, ProjectInfo, bool> predicate, string export, string title)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        var characters = (await characterRepository.LoadCharactersWithGroups(projectId)).Where(c => predicate(c, projectInfo)).ToList();

#pragma warning disable CS0612 // Type or member is obsolete
        var project = await GetProjectFromList(projectId, characters);
#pragma warning restore CS0612 // Type or member is obsolete

        var list = new CharacterListViewModel(CurrentUserId, title, characters, project, projectInfo, problemValidator);

        var exportType = ExportTypeNameParserHelper.ToExportType(export);

        if (exportType == null)
        {
            return View("Index", list);
        }

        return Export(list, exportType.Value, projectInfo);
    }

    [HttpGet("~/{ProjectId}/characters/bygroup/{CharacterGroupId}")]
    public async Task<ActionResult> ByGroup(ProjectIdentification projectId, int characterGroupId, string export)
    {
        var characterGroup = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);

        if (characterGroup == null)
        {
            return NotFound();
        }

        var groupIds = characterGroup.GetChildrenGroupsIdRecursiveIncludingThis();
        var characters = (await ProjectRepository.GetCharacterByGroups(projectId, groupIds)).Where(ch => ch.IsActive).ToList();

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var list = new CharacterListByGroupViewModel(CurrentUserId,
          characters, characterGroup, projectInfo, problemValidator);

        var exportType = ExportTypeNameParserHelper.ToExportType(export);

        if (exportType is null)
        {
            return View("ByGroup", list);
        }

        return Export(list, exportType.Value, projectInfo);
    }

    [HttpGet]
    public async Task<ActionResult> ByAssignedField(int projectfieldId, ProjectIdentification projectId, string export)
    {
        var pi = await projectMetadataRepository.GetProjectMetadata(projectId);
        var field = pi.GetFieldById(new ProjectFieldIdentification(projectId, projectfieldId));

        return await MasterCharacterList(
          projectId,
          (character, projectInfo) => character.IsActive && character.GetFields(projectInfo).Single(f => f.Field.Id == field.Id).HasEditableValue,
          export,
          "Поле (проставлено): " + field.Name);
    }

    [HttpGet]
    public Task<ActionResult> Vacant(ProjectIdentification projectid, string export)
      => MasterCharacterList(projectid, (character, projectInfo) => character.ApprovedClaim == null && character.IsActive, export, "Свободные персонажи");

    [HttpGet]
    public Task<ActionResult> WithPlayers(ProjectIdentification projectid, string export)
      => MasterCharacterList(projectid, (character, projectInfo) => character.ApprovedClaim != null && character.IsActive, export, "Занятые персонажи");

    private FileContentResult Export(CharacterListViewModel list, ExportType exportType, ProjectInfo projectInfo)
    {
        var generator = exportDataService.GetGenerator(
            exportType,
            list.Items,
          new CharacterListItemViewModelExporter(projectInfo, uriService));

        return GeneratorResultHelper.Result(list.ProjectName + ": " + list.Title, generator);
    }
}


