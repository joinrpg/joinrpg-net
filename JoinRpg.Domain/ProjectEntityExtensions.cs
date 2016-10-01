using System;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static class ProjectEntityExtensions
  {
    public static bool HasMasterAccess([NotNull] this IProjectEntity entity, int? currentUserId, Func<ProjectAcl, bool> requiredAccess)
    {
      if (entity == null) throw new ArgumentNullException(nameof(entity));
      return entity.Project.ProjectAcls.Where(requiredAccess).Any(pa => pa.UserId == currentUserId);
    }

    public static bool HasMasterAccess([NotNull] this IProjectEntity entity, int? currentUserId)
    {
      if (entity == null) throw new ArgumentNullException(nameof(entity));
      return entity.HasMasterAccess(currentUserId, acl => true);
    }

    public static void RequestMasterAccess(this IProjectEntity field, int? currentUserId, Expression<Func<ProjectAcl, bool>> lambda)
    {
      if (field == null)
      {
        throw new ArgumentNullException(nameof(field));
      }
      if (field.Project == null)
      {
        throw new ArgumentNullException(nameof(field.Project));
      }
      if (!field.HasMasterAccess(currentUserId, acl => lambda.Compile()(acl)))
      {
        throw new NoAccessToProjectException(field.Project, currentUserId, lambda);
      }
    }

    public static void RequestMasterAccess(this IProjectEntity field, int currentUserId)
    {
      if (field == null)
      {
        throw new ArgumentNullException(nameof(field));
      }
      if (field.Project == null)
      {
        throw new ArgumentNullException(nameof(field.Project));
      }
      if (!field.HasMasterAccess(currentUserId))
      {
        throw new NoAccessToProjectException(field.Project, currentUserId);
      }
    }

    public static void EnsureActive<T>(this T entity) where T:IDeletableSubEntity, IProjectEntity
    {
      if (!entity.IsActive)
      {
        throw new ProjectEntityDeactivedException(entity);
      }
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
             (character.Project.Details?.PublishPlot ?? false);
    }

    public static bool HasPlayerAccesToClaim(this Claim claim, int? currentUserIdOrDefault)
    {
      return claim.PlayerUserId == currentUserIdOrDefault;
    }

    public static bool HasEditRolesAccess(this IProjectEntity character, int? currentUserId)
    {
      return character.HasMasterAccess(currentUserId, s => s.CanEditRoles) && character.Project.Active;
    }

    public static void EnsureProjectActive(this IProjectEntity character)
    {
      if (!character.Project.Active)
      {
        throw new ProjectDeactivedException();
      }
    }
  }
}