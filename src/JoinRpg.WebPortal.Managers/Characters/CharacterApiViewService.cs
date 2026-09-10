using System.Text.Json;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Web.Models.Characters;
using JoinRpg.XGameApi.Contract;
using CharacterHeader = JoinRpg.XGameApi.Contract.CharacterHeader;
// Не путать с доменным CharacterInfo (ADR013): здесь CharacterInfo — DTO внешнего API,
// а доменный агрегат называется DomainCharacterInfo.
using CharacterInfo = JoinRpg.XGameApi.Contract.CharacterInfo;
using DomainCharacterInfo = JoinRpg.DomainTypes.Characters.CharacterInfo;

namespace JoinRpg.WebPortal.Managers.Characters;

internal class CharacterApiViewService(
    ICharacterRepository characterRepository,
    ICharacterInfoRepository characterInfoRepository,
    IUserRepository userRepository,
    ICharacterService characterService,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor
        ) : ICharacterApiViewService
{
    public async Task<IReadOnlyCollection<CharacterHeader>> GetCharacterHeaders(ProjectIdentification projectId, DateTime? modifiedSince)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        _ = projectInfo.RequestMasterAccess(currentUserAccessor);

        var characters = await characterRepository.GetCharacterHeaders(projectId.Value, modifiedSince);
        return [.. characters.Select(character => BuildCharacterHeader(projectId, character.CharacterId, character.UpdatedAt, character.IsActive))];
    }

    private static CharacterHeader BuildCharacterHeader(ProjectIdentification projectId, int characterId, DateTime updatedAt, bool isActive) =>
        new CharacterHeader
        {
            CharacterId = characterId,
            UpdatedAt = updatedAt,
            IsActive = isActive,
            CharacterLink = $"/x-game-api/{projectId.Value}/characters/{characterId}/",
        };

    public async Task<CharacterInfo> GetCharacterInfo(CharacterIdentification characterId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(characterId.ProjectId);
        _ = projectInfo.RequestMasterAccess(currentUserAccessor);

        var character = await characterInfoRepository.GetCharacterInfo(characterId);

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

    public async Task<CharacterHeader> CreateCharacter(ProjectIdentification projectId, CreateCharacterRequest request)
    {
        var characterTypeInfo = CreateCharacterRequestMapper.ToCharacterTypeInfo(request);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var fieldsToSet = new FieldLayerContainer(
            projectInfo,
            FieldValueConverter.ConvertToStringValues(request.FieldValues));

        var characterId = await characterService.AddCharacter(new AddCharacterRequest(
            projectId,
            [],
            characterTypeInfo,
            fieldsToSet));

        var character = await characterInfoRepository.GetCharacterInfo(characterId);
        return BuildCharacterHeader(projectId, character.Id.CharacterId, character.UpdatedAt, character.IsActive);
    }

    public async Task SetCharacterFields(CharacterIdentification characterId, Dictionary<int, JsonElement> fieldValues)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(characterId.ProjectId);

        var fieldsToSet = new FieldLayerContainer(
            projectInfo,
            FieldValueConverter.ConvertToStringValues(fieldValues));

        await characterService.SetFields(characterId, fieldsToSet);
    }
}
