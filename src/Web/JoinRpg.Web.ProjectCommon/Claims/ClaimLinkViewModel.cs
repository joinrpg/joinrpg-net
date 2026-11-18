namespace JoinRpg.Web.ProjectCommon.Claims;

public record class ClaimLinkViewModel(ClaimIdentification ClaimId, UserDisplayName PlayerName, string CharacterName, string OtherPlayerNicks, UserIdentification UserId)
{
    public UserLinkViewModel ToUserLinkViewModel() => new UserLinkViewModel(UserId, PlayerName.DisplayName, ViewMode.Show);
}

