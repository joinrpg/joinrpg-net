using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Helpers;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Exporters;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[MasterAuthorize()]
[Route("{projectId}/characters/[action]")]
public class CharacterListController(
    IExportDataService exportDataService,
    IUriService uriService,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterProblemValidator problemValidator,
    ICharacterInfoRepository characterInfoRepository,
    ICharacterGroupRepository charGroupRepository,
    IUserRepository userRepository,
    IClaimInfoRepository claimInfoRepository,
    ICurrentUserAccessor currentUserAccessor
    ) : JoinControllerGameBase
{
    [HttpGet]
    public Task<ActionResult> Active(ProjectIdentification projectid, string export)
     => MasterCharacterList(projectid, CharacterStatusSpec.Active, character => true, export, "Все персонажи");

    [HttpGet]
    public Task<ActionResult> Deleted(ProjectIdentification projectId, string export)
      => MasterCharacterList(projectId, CharacterStatusSpec.Deleted, character => true, export, "Удаленные персонажи");


    [HttpGet]
    public Task<ActionResult> Problems(ProjectIdentification projectid, string export)
      => MasterCharacterList(projectid, CharacterStatusSpec.Active,
        character => problemValidator.Validate(character).Any(), export,
        "Проблемные персонажи");

    [HttpGet]
    public async Task<ActionResult> ByUnAssignedField(int projectfieldId, ProjectIdentification projectId, string export)
    {
        var pi = await projectMetadataRepository.GetProjectMetadata(projectId);
        var field = pi.GetFieldById(new ProjectFieldIdentification(projectId, projectfieldId));

        return await MasterCharacterList(projectId, CharacterStatusSpec.Active,
          character => problemValidator.ValidateFieldOnly(character, field.Id).Any(),
          export,
          "Поле (непроставлено): " + field.Name);
    }

    /// <param name="spec">
    /// Какие персонажи нужны списку. Отбор по «удалён / не удалён» уезжает в запрос: собирать
    /// агрегат (поля, заявки) на персонажах, которых список всё равно не покажет, незачем.
    /// </param>
    /// <param name="predicate">
    /// Доменный фильтр поверх уже загруженных персонажей — то, что в SQL не выразить (проблемы,
    /// значения полей).
    /// </param>
    private async Task<ActionResult> MasterCharacterList(
        ProjectIdentification projectId,
        CharacterStatusSpec spec,
        Func<CharacterInfo, bool> predicate,
        string export,
        string title)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var characters = (await characterInfoRepository.GetAllCharacterInfos(projectId, spec)).Where(predicate).ToList();
        var approvedClaims = await claimInfoRepository.GetApprovedClaimInfos(characters);
        var fieldUsers = await userRepository.LoadFieldUserLinks(characters);

        var list = new CharacterListViewModel(currentUserAccessor.UserIdentification, title, characters, approvedClaims, fieldUsers, projectInfo, problemValidator);

        var exportType = ExportTypeNameParserHelper.ToExportType(export);

        if (exportType == null)
        {
            return View("Index", list);
        }

        return Export(list, exportType.Value, projectInfo);
    }

    [HttpGet("~/{ProjectId}/characters/bygroup/{characterGroupId}")]
    public async Task<ActionResult> ByGroup(ProjectIdentification projectId, int characterGroupId, string export)
    {
        var characterGroupIdentification = new CharacterGroupIdentification(projectId, characterGroupId);
        var characterGroup = await charGroupRepository.GetCharacterGroupFullInfo(characterGroupIdentification);

        if (characterGroup == null)
        {
            return NotFound();
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var groupIds = projectInfo.GroupTree.GetChildGroupIdsIncludingThis(characterGroupIdentification).ToList();
        var characters = await characterInfoRepository.GetCharacterInfosByGroups(projectId, groupIds, CharacterStatusSpec.Active);
        var approvedClaims = await claimInfoRepository.GetApprovedClaimInfos(characters);
        var fieldUsers = await userRepository.LoadFieldUserLinks(characters);

        var list = new CharacterListByGroupViewModel(currentUserAccessor.UserIdentification,
          characters, approvedClaims, fieldUsers, characterGroup, projectInfo, problemValidator);

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
          CharacterStatusSpec.Active,
          character => character.GetAllFields().Single(f => f.Field.Id == field.Id).HasEditableValue,
          export,
          "Поле (проставлено): " + field.Name);
    }

    [HttpGet]
    public Task<ActionResult> Vacant(ProjectIdentification projectid, string export)
      => MasterCharacterList(projectid, CharacterStatusSpec.Active, character => character.ApprovedClaimId is null, export, "Свободные персонажи");

    [HttpGet]
    public Task<ActionResult> WithPlayers(ProjectIdentification projectid, string export)
      => MasterCharacterList(projectid, CharacterStatusSpec.Active, character => character.ApprovedClaimId is not null, export, "Занятые персонажи");

    private FileContentResult Export(CharacterListViewModel list, ExportType exportType, ProjectInfo projectInfo)
    {
        var generator = exportDataService.GetGenerator(
            exportType,
            list.Items,
          new CharacterListItemViewModelExporter(projectInfo, uriService));

        return GeneratorResultHelper.Result(list.ProjectName + ": " + list.Title, generator);
    }
}

