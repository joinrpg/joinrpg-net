using System.Text.Json;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Web.Models.Characters;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;
using CharacterHeader = JoinRpg.XGameApi.Contract.CharacterHeader;
// Не путать с доменным CharacterInfo (ADR013): здесь CharacterInfo — DTO внешнего API,
// а доменный агрегат называется DomainCharacterInfo.
using CharacterInfo = JoinRpg.XGameApi.Contract.CharacterInfo;
using DomainCharacterInfo = JoinRpg.DomainTypes.Characters.CharacterInfo;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-game-api/{projectId}/characters"), XGameMasterAuthorize()]
public class CharacterApiController(
    ICharacterRepository characterRepository,
    ICharacterInfoRepository characterInfoRepository,
    IUserRepository userRepository,
    ICharacterService characterService,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor
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
        var character = await characterInfoRepository.GetCharacterInfo(
            new CharacterIdentification(new ProjectIdentification(projectId), characterId));

        // ProjectInfo несёт сам агрегат — отдельный запрос метаданных не нужен.
        var access = AccessArgumentsFactory.Create(character, currentUserAccessor);
        var fields = character.GetFieldLayers(access);

        return
            new CharacterInfo
            {
                CharacterId = character.Id.CharacterId,
                UpdatedAt = character.UpdatedAt,
                IsActive = character.IsActive,
                InGame = character.InGame,
                BusyStatus = (CharacterBusyStatus)character.GetBusyStatus(),
                Groups = ApiInfoBuilder.ToGroupHeaders([.. character.DirectGroups]),
                AllGroups = ApiInfoBuilder.ToGroupHeaders([.. character.ParentGroupsToTop]),
                Fields = [.. fields.GetSortedFieldsForView().Select(ApiInfoBuilder.ToFieldValue)],
#pragma warning disable CS0612 // Type or member is obsolete
                PlayerUserId = character.ApprovedClaim?.PlayerId.Value,
#pragma warning restore CS0612 // Type or member is obsolete
                CharacterDescription = character.Description.Value,
                CharacterName = character.CharacterName,
                PlayerInfo = await CreatePlayerInfo(character),
            };
    }

    private async Task<CharacterPlayerInfo?> CreatePlayerInfo(
        DomainCharacterInfo character)
    {
        if (character.ApprovedClaim is not { } approvedClaim)
        {
            return null;
        }

        var player = await userRepository.GetRequiredUserInfo(approvedClaim.PlayerId);
        return ApiInfoBuilder.CreatePlayerInfo(character, approvedClaim, player);
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

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(projectId));

        FieldLayerContainer fieldsToSet;
        try
        {
            fieldsToSet = new FieldLayerContainer(
                projectInfo,
                FieldValueConverter.ConvertToStringValues(request.FieldValues));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            // Поля, которого нет в проекте, раньше хватало на 500 из глубины домена.
            return BadRequest(ex.Message);
        }

        var characterId = await characterService.AddCharacter(new AddCharacterRequest(
            new ProjectIdentification(projectId),
            [],
            characterTypeInfo,
            fieldsToSet));

        var character = await characterInfoRepository.GetCharacterInfo(characterId);
        return CreatedAtAction(
            nameof(GetOne),
            new { projectId, characterId = characterId.CharacterId },
            BuildCharacterHeader(projectId, character.Id.CharacterId, character.UpdatedAt, character.IsActive));
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
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(projectId));

        FieldLayerContainer fieldsToSet;
        try
        {
            fieldsToSet = new FieldLayerContainer(
                projectInfo,
                FieldValueConverter.ConvertToStringValues(fieldValues));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            // Поля, которого нет в проекте, раньше хватало на 500 из глубины домена.
            return BadRequest(ex.Message);
        }
        try
        {
            await characterService.SetFields(new CharacterIdentification(projectId, characterId), fieldsToSet);
        }
        catch (FieldCannotHaveValueException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (FieldValueInvalidException ex)
        {
            return BadRequest(ex.Message);
        }
        return "ok";
    }

}
