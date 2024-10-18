using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.WebComponents;


namespace JoinRpg.Web.Models.Characters;
public static class CharacterViewModeExtensions
{
    public static ViewMode GetViewModeForCharacter(this Character character, int? currentUserIdOrDefault)
    {
        var hasAccess = character.HasAnyAccess(currentUserIdOrDefault);
        return (character.HidePlayerForCharacter, hasAccess, character.Project.Details.PublishPlot)
            switch
        {
            (false, _, _) => ViewMode.Show,
            (true, false, false) => ViewMode.Hide,
            (true, _, true) => ViewMode.ShowAsPrivate,
            (true, true, _) => ViewMode.ShowAsPrivate,
        };
    }

    public static UserLinkViewModel? GetCharacterPlayerLinkViewModel(this Character character, int? currentUserIdOrDefault)
    {
        return UserLinks.Create(character.ApprovedClaim?.Player, character.GetViewModeForCharacter(currentUserIdOrDefault));
    }
}
