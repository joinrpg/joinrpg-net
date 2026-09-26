using System.Text.Json;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Users;
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
        return MapToDto(character, await LoadPlayersAsync([character]));
    }

    public async Task<IReadOnlyCollection<CharacterInfo>> GetCharactersByIds(ProjectIdentification projectId, IReadOnlyCollection<int> characterIds)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        _ = projectInfo.RequestMasterAccess(currentUserAccessor);

        var characters = await characterInfoRepository.GetCharacterInfos(
            [.. characterIds.Select(id => new CharacterIdentification(projectId, id))]);
        return await MapAllToDto(characters);
    }

    public async Task<IReadOnlyCollection<CharacterInfo>> ListCharactersByGroup(CharacterGroupIdentification groupId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(groupId.ProjectId);
        _ = projectInfo.RequestMasterAccess(currentUserAccessor);

        var groupIds = projectInfo.GroupTree.GetChildGroupIdsIncludingThis(groupId);
        var characters = await characterInfoRepository.GetCharacterInfosByGroups(groupId.ProjectId, groupIds);
        return await MapAllToDto(characters);
    }

    /// <summary>
    /// Игроки забираются одним запросом на всех персонажей сразу.
    /// </summary>
    /// <remarks>
    /// Здесь был <c>Task.WhenAll(characters.Select(MapToDto))</c>, а внутри каждого маппинга —
    /// свой поход в БД за игроком. DAL у нас на EF6, где контекст не потокобезопасен и
    /// перекрывающихся асинхронных операций не допускает: на группе с несколькими сыгранными
    /// ролями это падало с «A second operation started on this context» и уводило соединение в
    /// закрытое состояние. Заодно N персонажей давали N запросов.
    /// </remarks>
    private async Task<IReadOnlyCollection<CharacterInfo>> MapAllToDto(IReadOnlyCollection<DomainCharacterInfo> characters)
    {
        var players = await LoadPlayersAsync(characters);
        return [.. characters.Select(character => MapToDto(character, players))];
    }

    private async Task<IReadOnlyDictionary<UserIdentification, UserInfo>> LoadPlayersAsync(
        IReadOnlyCollection<DomainCharacterInfo> characters)
    {
        UserIdentification[] playerIds = [..
            characters
                .Select(character => character.ApprovedClaim?.PlayerId)
                .OfType<UserIdentification>()
                .Distinct()];

        if (playerIds.Length == 0)
        {
            return new Dictionary<UserIdentification, UserInfo>();
        }

        var players = await userRepository.GetRequiredUserInfos(playerIds);
        return players.ToDictionary(player => player.UserId);
    }

    private CharacterInfo MapToDto(
        DomainCharacterInfo character,
        IReadOnlyDictionary<UserIdentification, UserInfo> players)
    {
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
                PlayerInfo = CreatePlayerInfo(character, players),
            };
    }

    private static CharacterPlayerInfo? CreatePlayerInfo(
        DomainCharacterInfo character,
        IReadOnlyDictionary<UserIdentification, UserInfo> players)
    {
        if (character.ApprovedClaim is not { } approvedClaim)
        {
            return null;
        }

        var player = players[approvedClaim.PlayerId];
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
