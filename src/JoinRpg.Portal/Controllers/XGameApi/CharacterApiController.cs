using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Web.Models.Characters;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;
using CharacterHeader = JoinRpg.XGameApi.Contract.CharacterHeader;
using GroupHeader = JoinRpg.XGameApi.Contract.GroupHeader;

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
                Groups = ToGroupHeaders(character.DirectGroups),
                AllGroups = ToGroupHeaders(character.AllGroups),
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
                PlayerInfo = character.ApprovedClaim is null ? null :
                    CreatePlayerInfo(character.ApprovedClaim, projectInfo),
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
        await characterService.SetFields(projectId, characterId, fieldValues);
        return "ok";
    }

    private static IOrderedEnumerable<GroupHeader> ToGroupHeaders(
        IReadOnlyCollection<Data.Interfaces.GroupHeader> characterDirectGroups)
    {
        return characterDirectGroups.Where(group => group.IsActive && !group.IsSpecial)
            .Select(
                group => new GroupHeader
                {
                    CharacterGroupId = group.CharacterGroupId,
                    CharacterGroupName = group.CharacterGroupName,
                })
            .OrderBy(group => group.CharacterGroupId);
    }

    private static CharacterPlayerInfo CreatePlayerInfo(Claim claim, ProjectInfo projectInfo)
    {
        return new CharacterPlayerInfo(
                                    claim.PlayerUserId,
                                    claim.ClaimFeeDue(projectInfo) <= 0,
                                    claim.Player.ExtractDisplayName().DisplayName,
                                    new PlayerContacts(
                                        claim.Player.Email,
                                        claim.Player.Extra?.PhoneNumber,
                                        claim.Player.Extra?.VkVerified == true ? claim.Player.Extra?.Vk : null,
                                        claim.Player.Extra?.Telegram)
                                    );
    }

}
