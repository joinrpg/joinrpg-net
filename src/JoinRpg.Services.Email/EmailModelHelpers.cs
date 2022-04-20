using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Email
{
    internal static class EmailModelHelpers
    {

        public static string GetInitiatorString(this ClaimEmailModel model)
        {
            switch (model.InitiatorType)
            {
                case ParcipantType.Nobody:
                    return "";
                case ParcipantType.Master:
                    return $"мастером {model.Initiator.GetDisplayName()}";
                case ParcipantType.Player:
                    return "игроком";
                default:
                    throw new ArgumentOutOfRangeException(nameof(model.InitiatorType), model.InitiatorType, null);
            }
        }

        public static bool GetEmailEnabled(this EmailModelBase model) => !model.ProjectName.Trim().StartsWith("NOEMAIL");

        public static List<User> GetRecipients(this EmailModelBase model) => model.Recipients.Where(u => u != null && u.UserId != model.Initiator.UserId).Distinct().ToList();

        public static string GetPlayerList(this IEnumerable<Claim> claims)
        {
            var players = claims.Select(c => c.Player.GetDisplayName()).ToArray();

            if (!players.Any())
            {
                players = new[] { "n/a" };
            }

            return players.JoinStrings(" \n- ");
        }

        public static string GetClaimEmailTitle(this EmailModelBase model, Claim claim) => $"{model.ProjectName}: {claim.Name}, игрок {claim.Player.GetDisplayName()}";

        public static string GetClaimEmailTitle(this ClaimEmailModel model) => model.GetClaimEmailTitle(model.Claim);
    }
}
