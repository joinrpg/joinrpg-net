using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Claims;
public record class ClaimLinkViewModel(ClaimIdentification ClaimId, UserDisplayName PlayerName, string CharacterName, string OtherPlayerNicks);
