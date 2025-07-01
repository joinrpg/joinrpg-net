using System.Linq.Expressions;
using JoinRpg.DataModel;
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
        this Claim claim,
        int currentUserId,
        Expression<Func<ProjectAcl, bool>> access,
        ExtraAccessReason reasons = ExtraAccessReason.None)
    {
        if (claim?.Project == null)
        {
            throw new ArgumentNullException(nameof(claim));
        }

        if (!claim.HasAccess(currentUserId, access, reasons))
        {
            throw new NoAccessToProjectException(claim, currentUserId);
        }

        return claim;
    }

    public static Claim RequestAccess(
        this Claim claim,
        int currentUserId,
        Permission permission,
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

    public static Claim RequestAccess(
        this Claim claim,
        int currentUserId,
        ExtraAccessReason reasons = ExtraAccessReason.None) => claim.RequestAccess(currentUserId, acl => true, reasons);

    public static bool HasAccess(this Claim claim,
        int? userId,
        Expression<Func<ProjectAcl, bool>> permission,
        ExtraAccessReason reasons = ExtraAccessReason.None)
    {
        ArgumentNullException.ThrowIfNull(claim);

        ArgumentNullException.ThrowIfNull(permission);

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

        return claim.HasMasterAccess(userId, permission);
    }

    public static bool HasAccess(this Claim claim,
        int? userId,
        Permission permission,
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

        return claim.HasMasterAccess(userId, permission);
    }

    public static bool HasAccess(this Claim claim,
        int? userId,
        ExtraAccessReason reasons = ExtraAccessReason.None) => claim.HasAccess(userId, acl => true, reasons);
}
