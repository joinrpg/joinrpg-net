using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using JoinRpg.DataModel.Extensions;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.Access;

namespace JoinRpg.Domain;

public static class ProjectEntityExtensions
{
    [Pure]
    [Obsolete("Передавай сюда ProjectInfo")]
    public static bool HasMasterAccess(this IProjectEntity entity, int? currentUserId, Expression<Func<ProjectAcl, bool>> requiredAccess)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return entity.Project.ProjectAcls.Where(acl => requiredAccess.Compile()(acl)).Any(pa => pa.UserId == currentUserId);
    }

    [Pure]
    [Obsolete("Передавай сюда ProjectInfo")]
    public static bool HasMasterAccess(this IProjectEntity entity, ICurrentUserAccessor currentUserAccessor, Permission permission = Permission.None)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return currentUserAccessor.UserIdOrDefault is int userId && entity.Project.ProjectAcls.Where(acl => permission.GetPermssionExpression()(acl)).Any(pa => pa.UserId == userId);
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
    public static bool HasMasterAccess(this IProjectEntity entity, [NotNullWhen(true)] int? currentUserId, Permission permission = Permission.None)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return entity.Project.ProjectAcls.Where(acl => permission.GetPermssionExpression()(acl)).Any(pa => pa.UserId == currentUserId);
    }

    public static T RequestMasterAccess<T>(this T field, ICurrentUserAccessor currentUserAccessor, Permission permission = Permission.None)
            where T : IProjectEntity
    {
        return field.RequestMasterAccess(currentUserAccessor.UserId, permission);
    }

    public static T RequestMasterAccess<T>(this T field, [NotNull] int? currentUserId, Permission permission = Permission.None)
        where T : IProjectEntity
    {
        return field.RequestMasterAccess(currentUserId, acl => permission.GetPermssionExpression()(acl));
    }

    [Obsolete("Передавай сюда ProjectInfo")]
    public static T RequestMasterAccess<T>(this T field,
        [NotNull]
        int? currentUserId,
        Expression<Func<ProjectAcl, bool>>? accessType)
    where T : IProjectEntity
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(field.Project);

        if (currentUserId is null)
        {
            throw new NoAccessToProjectException(field.Project, currentUserId);
        }

        if (accessType == null)
        {
            if (!field.HasMasterAccess(currentUserId))
            {
                throw new NoAccessToProjectException(field.Project, currentUserId);
            }
        }
        else
        {
            if (!field.HasMasterAccess(currentUserId, acl => accessType.Compile()(acl)))
            {
                throw new NoAccessToProjectException(field.Project, currentUserId, accessType);
            }
        }

        return field;
    }

    public static T EnsureActive<T>([NotNull] this T? entity) where T : IDeletableSubEntity, IProjectEntity
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!entity.IsActive)
        {
            throw new ProjectEntityDeactivatedException(entity);
        }

        if (!entity.Project.Active)
        {
            throw new ProjectEntityDeactivatedException(entity);
        }

        return entity;
    }

    public static bool HasPlayerAccess(this Character character, int? currentUserId)
    {
        ArgumentNullException.ThrowIfNull(character);

        return currentUserId != null && character.ApprovedClaim?.PlayerUserId == currentUserId;
    }

    public static bool HasAnyAccess(this Character character, int? currentUserIdOrDefault)
    {
        ArgumentNullException.ThrowIfNull(character);

        return character.HasMasterAccess(currentUserIdOrDefault) || character.HasPlayerAccess(currentUserIdOrDefault);
    }

    public static bool HasPlotViewAccess(this Character character, int? currentUserIdOrDefault)
    {
        return character.HasMasterAccess(currentUserIdOrDefault) || character.HasPlayerAccess(currentUserIdOrDefault) ||
               character.Project.Details.PublishPlot;
    }

    public static bool HasPlayerAccesToClaim(this Claim claim, int? currentUserIdOrDefault)
    {
        ArgumentNullException.ThrowIfNull(claim);

        return claim.PlayerUserId == currentUserIdOrDefault;
    }

    public static bool HasEditRolesAccess(this IProjectEntity character, int? currentUserId) => character.HasMasterAccess(currentUserId, s => s.CanEditRoles) && character.Project.Active;

    [Obsolete("Передавай сюда ProjectInfo")]
    public static T EnsureProjectActive<T>(this T entity)
  where T : IProjectEntity => !entity.Project.Active ? throw new ProjectDeactivatedException() : entity;

    public static void RequestAnyAccess(this CommentDiscussion discussion, int currentUserId)
    {
        if (!discussion.HasAnyAccess(currentUserId))
        {
            throw new NoAccessToProjectException(discussion, currentUserId);
        }
    }

    public static bool HasAnyAccess(this CommentDiscussion discussion, int currentUserId) => discussion.HasMasterAccess(currentUserId) || discussion.HasPlayerAccess(currentUserId);

    public static bool HasPlayerAccess(this CommentDiscussion commentDiscussion, int currentUserId)
    {
        var forumThread =
          commentDiscussion.GetForumThread();

        var claim =
          commentDiscussion.GetClaim();
        if (forumThread != null)
        {
            return forumThread.HasPlayerAccess(currentUserId);
        }
        if (claim != null)
        {
            return claim.HasPlayerAccesToClaim(currentUserId);
        }
        throw new InvalidOperationException();
    }

    [Pure]
    public static bool HasPlayerAccess(this IForumThread forumThread, int? currentUserId)
    {
        ArgumentNullException.ThrowIfNull(forumThread);

        return currentUserId != null && forumThread.IsVisibleToPlayer &&
               forumThread.Project.Claims.OfUserApproved((int)currentUserId)
                 .Any(c => c.Character.IsPartOfGroup(forumThread.CharacterGroupId));
    }

    [Pure]
    public static bool HasAnyAccess(this IForumThread forumThread, int? currentUserId)
    {
        ArgumentNullException.ThrowIfNull(forumThread);

        return forumThread.HasMasterAccess(currentUserId) || forumThread.HasPlayerAccess(currentUserId);
    }

    [Pure]
    public static Claim? GetClaim(this CommentDiscussion commentDiscussion)
    {
        return commentDiscussion.Project.Claims.SingleOrDefault(
          c => c.CommentDiscussionId == commentDiscussion.CommentDiscussionId);
    }

    [Pure]
    public static ForumThread? GetForumThread(this CommentDiscussion commentDiscussion)
    {
        return commentDiscussion.Project.ForumThreads.SingleOrDefault(
          ft => ft.CommentDiscussionId == commentDiscussion.CommentDiscussionId);
    }
}
