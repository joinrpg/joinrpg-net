using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Email;

internal static class EmailModelHelpers
{

    public static string GetInitiatorString(this ClaimEmailModel model)
    {
        return model.InitiatorType switch
        {
            ParcipantType.Nobody => "",
            ParcipantType.Master => $"мастером {model.Initiator.GetDisplayName()}",
            ParcipantType.Player => "игроком",
            _ => throw new ArgumentOutOfRangeException(nameof(model.InitiatorType), model.InitiatorType, null),
        };
    }

    public static bool GetEmailEnabled(this EmailModelBase model) => !model.ProjectName.Trim().StartsWith("NOEMAIL");

    public static List<User> GetRecipients(this EmailModelBase model) => model.Recipients.Where(u => u != null && u.UserId != model.Initiator.UserId).Distinct().ToList();

    public static string GetPlayerList(this IEnumerable<Claim> claims)
    {
        var players = claims.Select(c => c.Player.GetDisplayName()).ToArray();

        if (players.Length == 0)
        {
            players = ["n/a"];
        }

        return players.JoinStrings(" \n- ");
    }

    public static string GetClaimEmailTitle(this EmailModelBase model, Claim claim) => $"{model.ProjectName}: {claim.Name}, игрок {claim.Player.GetDisplayName()}";

    public static string GetClaimEmailTitle(this ClaimEmailModel model) => model.GetClaimEmailTitle(model.Claim);
}
