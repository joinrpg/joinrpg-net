using System.Text.Json;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Web.Models.Characters;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;
using CharacterHeader = JoinRpg.XGameApi.Contract.CharacterHeader;
using FieldCannotHaveValueException = JoinRpg.Domain.FieldCannotHaveValueException;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-game-api/{projectId}/characters"), XGameMasterAuthorize()]
public class CharacterApiController(
    ICharacterRepository characterRepository,
    ICharacterService characterService,
    IProjectMetadataRepository projectMetadataRepository
        ) : XGameApiController()
{

    /// <summary>
    /// Load character list. If you aggressively pull characters,
    /// please use modifiedSince parameter.
    /// </summary>
    [HttpGet]
    [Route("")]
    public async Task<IEnumerable<CharacterHeader>> GetList(int projectId,
        [FromQuery]
        DateTime? modifiedSince = null)
    {
        return (await characterRepository.GetCharacterHeaders(projectId, modifiedSince))
            .Select(character => BuildCharacterHeader(projectId, character.CharacterId, character.UpdatedAt, character.IsActive));
    }

    private static CharacterHeader BuildCharacterHeader(int projectId, int characterId, DateTime updatedAt, bool isActive) =>
        new CharacterHeader
        {
            CharacterId = characterId,
            UpdatedAt = updatedAt,
            IsActive = isActive,
            CharacterLink = $"/x-game-api/{projectId}/characters/{characterId}/",
        };

    /// <summary>
    /// Character details
    /// </summary>
    [HttpGet]
    [Route("{characterId}/")]
    public async Task<CharacterInfo> GetOne(int projectId, int characterId)
    {
        var character = await characterRepository.GetCharacterViewAsync(projectId, characterId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        return
            new CharacterInfo
            {
                CharacterId = character.CharacterId,
                UpdatedAt = character.UpdatedAt,
                IsActive = character.IsActive,
                InGame = character.InGame,
                BusyStatus = (CharacterBusyStatus)character.GetBusyStatus(),
                Groups = ApiInfoBuilder.ToGroupHeaders(character.DirectGroups),
                AllGroups = ApiInfoBuilder.ToGroupHeaders(character.AllGroups),
                Fields = [..character.GetFields(projectInfo).Where(field => field.HasViewableValue)
                    .Select(field => new FieldValue
                    {
                        ProjectFieldId = field.Field.Id.ProjectFieldId,
                        Value = field.Value,
                        DisplayString = field.DisplayString,
                    })],
#pragma warning disable CS0612 // Type or member is obsolete
                PlayerUserId = character.ApprovedClaim?.PlayerUserId,
#pragma warning restore CS0612 // Type or member is obsolete
                CharacterDescription = character.Description,
                CharacterName = character.Name,
                PlayerInfo = ApiInfoBuilder.CreatePlayerInfo(character.ApprovedClaim, projectInfo),
            };
    }

    /// <summary>
    /// Create new character
    /// </summary>
    [HttpPost]
    [Route("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<CharacterHeader>> CreateCharacter(int projectId, [FromBody] CreateCharacterRequest request)
    {
        CharacterTypeInfo characterTypeInfo;
        try
        {
            characterTypeInfo = CreateCharacterRequestMapper.ToCharacterTypeInfo(request);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        Dictionary<int, string?> convertedFields;
        try
        {
            convertedFields = FieldValueConverter.ConvertToStringValues(request.FieldValues);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        var characterId = await characterService.AddCharacter(new AddCharacterRequest(
            new ProjectIdentification(projectId),
            [],
            characterTypeInfo,
            convertedFields));

        var characterView = await characterRepository.GetCharacterViewAsync(projectId, characterId.CharacterId);
        return CreatedAtAction(
            nameof(GetOne),
            new { projectId, characterId = characterId.CharacterId },
            BuildCharacterHeader(projectId, characterView.CharacterId, characterView.UpdatedAt, characterView.IsActive));
    }

    /// <summary>
    /// Allows to set character fields as master
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="characterId">Character ID</param>
    /// <param name="fieldValues">
    /// Key = FieldId, Value = field value (for Select/Multiselect - id of value)
    /// Skipped values will be left unchanged</param>
    [HttpPost]
    [Route("{characterId}/fields")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<string>> SetCharacterFields(int projectId, int characterId, [FromBody] Dictionary<int, JsonElement> fieldValues)
    {
        Dictionary<int, string?> converted;
        try
        {
            converted = FieldValueConverter.ConvertToStringValues(fieldValues);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        try
        {
            await characterService.SetFields(new CharacterIdentification(projectId, characterId), converted);
        }
        catch (FieldCannotHaveValueException ex)
        {
            return BadRequest(ex.Message);
        }
        return "ok";
    }

}
