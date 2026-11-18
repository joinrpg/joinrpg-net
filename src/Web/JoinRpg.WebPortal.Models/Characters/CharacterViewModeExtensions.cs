using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Models.Characters;

public static class CharacterViewModeExtensions
{
    /// <summary>
    /// Как показывать игрока персонажа, считается поверх доменного агрегата (ADR013).
    /// <c>ProjectInfo</c> отдельным параметром не нужен — агрегат несёт его в себе.
    /// </summary>
    public static ViewMode GetViewModeForCharacter(this CharacterInfo character, UserIdentification? currentUserIdOrDefault)
    {
        // Зеркало Character.HasAnyAccess: доступ есть у мастера и у игрока принятой заявки.
        var hasAccess = character.ProjectInfo.HasMasterAccess(currentUserIdOrDefault)
            || (currentUserIdOrDefault is not null && character.ApprovedClaim?.PlayerId == currentUserIdOrDefault);

        return (character.HidePlayerForCharacter, hasAccess, character.ProjectInfo.PublishPlot)
            switch
        {
            (false, _, _) => ViewMode.Show,
            (true, false, false) => ViewMode.Hide,
            (true, _, true) => ViewMode.ShowAsPrivate,
            (true, true, _) => ViewMode.ShowAsPrivate,
        };
    }

    public static UserLinkViewModel? GetCharacterPlayerLinkViewModel(this CharacterInfo character, UserIdentification? currentUserIdOrDefault)
        => character.ApprovedClaim?.Player
            .ToUserLinkViewModel(character.GetViewModeForCharacter(currentUserIdOrDefault));
}
