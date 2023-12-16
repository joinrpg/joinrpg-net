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

namespace JoinRpg.Web.Controllers.XGameApi;

[Route("x-game-api/{projectId}/characters"), XGameMasterAuthorize()]
public class CharacterApiController : XGameApiController
{
    private readonly IProjectMetadataRepository projectMetadataRepository;

    private ICharacterRepository CharacterRepository { get; }
    private ICharacterService CharacterService { get; }

    public CharacterApiController(
        IProjectRepository projectRepository,
        ICharacterRepository characterRepository,
        ICharacterService characterService,
        IProjectMetadataRepository projectMetadataRepository
        ) : base(projectRepository)
    {
        CharacterRepository = characterRepository;
        CharacterService = characterService;
        this.projectMetadataRepository = projectMetadataRepository;
    }

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
        return (await CharacterRepository.GetCharacterHeaders(projectId, modifiedSince)).Select(
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
        var character = await CharacterRepository.GetCharacterViewAsync(projectId, characterId);
        var project = await ProjectRepository.GetProjectWithFieldsAsync(projectId);
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
                Fields = character.GetFields(projectInfo, project).Where(field => field.HasViewableValue)
                    .Select(field => new FieldValue
                    {
                        ProjectFieldId = field.Field.ProjectFieldId,
                        Value = field.Value,
                        DisplayString = field.DisplayString,
                    }),
#pragma warning disable CS0612 // Type or member is obsolete
                PlayerUserId = character.ApprovedClaim?.PlayerUserId,
                CharacterDescription = character.Description,
#pragma warning restore CS0612 // Type or member is obsolete
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
    [XGameMasterAuthorize()]
    [HttpPost]
    [Route("{characterId}/fields")]
    public async Task<string> SetCharacterFields(int projectId, int characterId, Dictionary<int, string?> fieldValues)
    {
        await CharacterService.SetFields(projectId, characterId, fieldValues);
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
                                    new PlayerContacts(
                                        claim.Player.Email,
                                        claim.Player.Extra?.PhoneNumber,
                                        claim.Player.Extra?.VkVerified == true ? claim.Player.Extra?.Vk : null,
                                        claim.Player.Extra?.Telegram)
                                    );
    }

}
