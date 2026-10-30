using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using JoinRpg.Interfaces;

namespace JoinRpg.Domain;

public static class ProjectEntityExtensions
{
    [Pure]
    [Obsolete("Передавай сюда ProjectInfo")]
    public static bool HasMasterAccess(this IProjectEntity entity, ICurrentUserAccessor currentUserAccessor, Permission permission = Permission.None)
    {
        return currentUserAccessor.UserIdentificationOrDefault is { } userId && entity.HasMasterAccess(userId, permission);
    }

    [Pure]
    public static bool HasMasterAccess(this ProjectInfo entity, ICurrentUserAccessor currentUserAccessor, Permission permission = Permission.None)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var userId = currentUserAccessor.UserIdOrDefault;

        return userId is not null && entity.HasMasterAccess(currentUserAccessor.UserIdentification, permission);
    }

    [Pure]
    [Obsolete("Передавай сюда ProjectInfo")]
    public static bool HasMasterAccess(this IProjectEntity entity, [NotNullWhen(true)] UserIdentification? currentUserId, Permission permission = Permission.None)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (currentUserId is null)
        {
            return false;
        }

        return entity.Project.ProjectAcls.Where(acl => permission.GetPermssionExpression()(acl)).Any(pa => pa.UserId == currentUserId);
    }

    public static ProjectInfo RequestMasterAccess(this ProjectInfo field, ICurrentUserAccessor currentUserAccessor, Permission permission = Permission.None)
    {
        return field.RequestMasterAccess(currentUserAccessor.UserIdentificationOrDefault, permission);
    }

    public static ProjectInfo RequestMasterAccess(this ProjectInfo projectInfo, [NotNull] UserIdentification? currentUserId, Permission permission = Permission.None)
    {
        if (currentUserId is null)
        {
            throw new NoAccessToProjectException(projectInfo, null);
        }

        if (!projectInfo.HasMasterAccess(currentUserId, permission))
        {
            throw new NoAccessToProjectException(projectInfo, currentUserId, permission);
        }

        return projectInfo;
    }

    public static bool HasPlayerAccesToClaim(this Claim claim, int? currentUserIdOrDefault)
    {
        ArgumentNullException.ThrowIfNull(claim);

        return claim.PlayerUserId == currentUserIdOrDefault;
    }
}
