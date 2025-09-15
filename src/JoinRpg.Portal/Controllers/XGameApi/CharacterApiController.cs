using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Web.Models.Characters;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;
using CharacterHeader = JoinRpg.XGameApi.Contract.CharacterHeader;

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
        return (await characterRepository.GetCharacterHeaders(projectId, modifiedSince)).Select(
            character =>
                new CharacterHeader
                {
                    CharacterId = character.CharacterId,
                    UpdatedAt = character.UpdatedAt,
                    IsActive = character.IsActive,
                    CharacterLink =
                        $"/x-game-api/{projectId}/characters/{character.CharacterId}/",
                });
    }

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
                Fields = character.GetFields(projectInfo).Where(field => field.HasViewableValue)
                    .Select(field => new FieldValue
                    {
                        ProjectFieldId = field.Field.Id.ProjectFieldId,
                        Value = field.Value,
                        DisplayString = field.DisplayString,
                    }),
#pragma warning disable CS0612 // Type or member is obsolete
                PlayerUserId = character.ApprovedClaim?.PlayerUserId,
#pragma warning restore CS0612 // Type or member is obsolete
                CharacterDescription = character.Description,
                CharacterName = character.Name,
                PlayerInfo = ApiInfoBuilder.CreatePlayerInfo(character.ApprovedClaim, projectInfo),
            };
    }

    /// <summary>
    /// Allows to set character fields as master
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="characterId">Character ID</param>
    /// <param name="fieldValues">
    /// Key = FieldId, Value = field value.
    /// Skipped values will be left unchanged</param>
    [HttpPost]
    [Route("{characterId}/fields")]
    public async Task<string> SetCharacterFields(int projectId, int characterId, Dictionary<int, string?> fieldValues)
    {
        await characterService.SetFields(new CharacterIdentification(projectId, characterId), fieldValues);
        return "ok";
    }

}
