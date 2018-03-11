using System;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static class ProjectEntityExtensions
  {
    [MustUseReturnValue]
    public static bool HasMasterAccess([NotNull] this IProjectEntity entity, int? currentUserId, Expression<Func<ProjectAcl, bool>> requiredAccess)
    {
      if (entity == null) throw new ArgumentNullException(nameof(entity));
      return entity.Project.ProjectAcls.Where(acl => requiredAccess.Compile()(acl)).Any(pa => pa.UserId == currentUserId);
    }

    public static bool HasMasterAccess([NotNull] this IProjectEntity entity, int? currentUserId)
    {
      if (entity == null) throw new ArgumentNullException(nameof(entity));
      return entity.HasMasterAccess(currentUserId, acl => true);
    }

      [NotNull]
      public static T RequestMasterAccess<T>([CanBeNull] this T field,
          int? currentUserId,
          [CanBeNull] Expression<Func<ProjectAcl, bool>> accessType = null)
      where T:IProjectEntity
      {
          if (field == null)
          {
              throw new ArgumentNullException(nameof(field));
          }

          if (field.Project == null)
          {
              throw new ArgumentNullException(nameof(field.Project));
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

      [NotNull]
      public static T EnsureActive<T>(this T entity) where T : IDeletableSubEntity, IProjectEntity
      {
          if (!entity.IsActive)
          {
              throw new ProjectEntityDeactivedException(entity);
          }

          if (!entity.Project.Active)
          {
              throw new ProjectEntityDeactivedException(entity);
          }

          return entity;
      }

      public static bool HasPlayerAccess([NotNull] this Character character, int? currentUserId)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));
      return currentUserId != null && character.ApprovedClaim?.PlayerUserId == currentUserId;
    }

    public static bool HasAnyAccess([NotNull] this Character character, int? currentUserIdOrDefault)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));
      return character.HasMasterAccess(currentUserIdOrDefault) || character.HasPlayerAccess(currentUserIdOrDefault);
    }

    public static bool HasPlotViewAccess(this Character character, int? currentUserIdOrDefault)
    {
      return character.HasMasterAccess(currentUserIdOrDefault) || character.HasPlayerAccess(currentUserIdOrDefault) ||
             character.Project.Details.PublishPlot;
    }

    public static bool HasPlayerAccesToClaim([NotNull] this Claim claim, int? currentUserIdOrDefault)
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      return claim.PlayerUserId == currentUserIdOrDefault;
    }

    public static bool HasEditRolesAccess(this IProjectEntity character, int? currentUserId)
    {
      return character.HasMasterAccess(currentUserId, s => s.CanEditRoles) && character.Project.Active;
    }

    public static T EnsureProjectActive<T>(this T entity)
    where T:IProjectEntity
    {
        return !entity.Project.Active ? throw new ProjectDeactivedException() : entity;
    }

    public static void RequestAnyAccess(this CommentDiscussion discussion, int currentUserId)
    {
      if (!discussion.HasAnyAccess(currentUserId))
      {
        throw new NoAccessToProjectException(discussion, currentUserId);
      }
    }

      public static bool HasAnyAccess(this CommentDiscussion discussion, int currentUserId)
      {
          return (discussion.HasMasterAccess(currentUserId) || discussion.HasPlayerAccess(currentUserId));
      }

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

    [MustUseReturnValue]
    public static bool HasPlayerAccess([NotNull] this IForumThread forumThread, int? currentUserId)
    {
      if (forumThread == null) throw new ArgumentNullException(nameof(forumThread));
      return currentUserId != null && forumThread.IsVisibleToPlayer &&
             forumThread.Project.Claims.OfUserApproved((int) currentUserId)
               .Any(c => c.IsPartOfGroup(forumThread.CharacterGroupId));
    }

    [MustUseReturnValue]
    public static bool HasAnyAccess([NotNull] this IForumThread forumThread, int? currentUserId)
    {
      if (forumThread == null) throw new ArgumentNullException(nameof(forumThread));
      return forumThread.HasMasterAccess(currentUserId) || forumThread.HasPlayerAccess(currentUserId);
    }

    [CanBeNull, Pure]
    public static Claim GetClaim(this CommentDiscussion commentDiscussion)
    {
      return commentDiscussion.Project.Claims.SingleOrDefault(
        c => c.CommentDiscussionId == commentDiscussion.CommentDiscussionId);
    }

    [CanBeNull, Pure]
    public static ForumThread GetForumThread(this CommentDiscussion commentDiscussion)
    {
      return commentDiscussion.Project.ForumThreads.SingleOrDefault(
        ft => ft.CommentDiscussionId == commentDiscussion.CommentDiscussionId);
    }
  }
}
