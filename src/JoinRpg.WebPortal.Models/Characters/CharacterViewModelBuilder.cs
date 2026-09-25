using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Markdown;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Models.Characters;

/// <summary>
/// Общее для публичных списков ролей: как показать персонажа и кому он виден.
/// </summary>
/// <remarks>
/// Используется и деревом ролей (<see cref="CharacterGroupListViewModel"/>), и плоским списком
/// горячих ролей (<see cref="HotCharactersViewModel"/>) — ответ должен быть одинаковым.
/// </remarks>
internal static class CharacterViewModelBuilder
{
    public static CharacterViewModel Build(
        CharacterInfo character,
        bool isFirstCopy,
        UserIdentification? currentUserId,
        ProjectInfo projectInfo)
        => new()
        {
            CharacterId = character.Id.CharacterId,
            CharacterName = character.CharacterName,
            IsFirstCopy = isFirstCopy,
            ApplyStatus = new CharacterApplyViewModel(
                character.Id,
                character.GetBusyStatus(),
                character.CharacterTypeInfo.SlotLimit,
                character.CharacterTypeInfo.IsHot,
                ClaimValidator.IsAvailableForPlayer(character, projectInfo)),
            Description = character.Description.ToHtmlString(),
            IsPublic = character.CharacterTypeInfo.IsNamePublic,
            IsActive = character.IsActive,
            ActiveClaimsCount = character.ActiveClaimsCount,
            PlayerLink = character.GetCharacterPlayerLinkViewModel(currentUserId),
            HasEditRolesAccess = projectInfo.HasEditRolesAccess(currentUserId),
            ProjectId = character.Id.ProjectId.Value,
        };

    /// <summary>Зеркало <c>WorldObjectExtensions.IsVisible</c> для персонажа поверх агрегата.</summary>
    public static bool IsVisible(CharacterInfo character, UserIdentification? currentUserId)
        => character.CharacterTypeInfo.IsNamePublic
            || character.ProjectInfo.PublishPlot
            || character.ProjectInfo.HasMasterAccess(currentUserId);

    /// <summary>Зеркало <c>WorldObjectExtensions.IsVisible</c> для группы поверх метаданных.</summary>
    public static bool IsVisible(CharacterGroupInfo group, UserIdentification? currentUserId, ProjectInfo projectInfo)
        => group.IsPublic || projectInfo.PublishPlot || projectInfo.HasMasterAccess(currentUserId);
}
