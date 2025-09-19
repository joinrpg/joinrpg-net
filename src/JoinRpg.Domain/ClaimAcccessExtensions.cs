using System.Linq.Expressions;
using JoinRpg.Interfaces;
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
    [Obsolete]
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

    [Obsolete]
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

        return claim.HasMasterAccess(userId, permission);
    }

    public static bool HasAccess(this Claim claim,
    ICurrentUserAccessor userId,
    Permission permission = Permission.None,
    ExtraAccessReason reasons = ExtraAccessReason.None) => claim.HasAccess(userId.UserIdOrDefault, Permission.None, reasons);
}
