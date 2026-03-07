using System.Diagnostics.CodeAnalysis;
using JoinRpg.PrimitiveTypes.Access;

namespace JoinRpg.Domain;

[Flags]
public enum ExtraAccessReason
{
    None,
    Player = 1 << 0,
    ResponsibleMaster = 1 << 1,
    PlayerOrResponsible = Player | ResponsibleMaster,
}


public static class ClaimAcccessExtensions
{
    public static Claim RequestAccess(
        [NotNull]
        this Claim? claim,
        int currentUserId,
        Permission permission = Permission.None,
        ExtraAccessReason reasons = ExtraAccessReason.None)
    {
        if (claim?.Project == null)
        {
            throw new ArgumentNullException(nameof(claim));
        }

        if (!claim.HasAccess(currentUserId, permission, reasons))
        {
            throw new NoAccessToProjectException(claim, currentUserId);
        }
        return claim;
    }

    public static bool HasAccess(this Claim claim,
        int? userId,
        Permission permission = Permission.None,
        ExtraAccessReason reasons = ExtraAccessReason.None)
    {
        ArgumentNullException.ThrowIfNull(claim);

        if (userId == null)
        {
            return false;
        }

        if (reasons.HasFlag(ExtraAccessReason.Player) && claim.PlayerUserId == userId)
        {
            return true;
        }

        if (reasons.HasFlag(ExtraAccessReason.ResponsibleMaster) && claim.ResponsibleMasterUserId == userId)
        {
            return true;
        }

        return claim.HasMasterAccess(UserIdentification.FromOptional(userId), permission);
    }

}
