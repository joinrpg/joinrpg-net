using System;
using System.Linq;
using System.Linq.Expressions;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static class ProjectEntityExtensions
  {
    public static bool HasMasterAccess(this IProjectEntity entity, int? currentUserId, Func<ProjectAcl, bool> requiredAccess)
    {
      return entity.Project.ProjectAcls.Where(requiredAccess).Any(pa => pa.UserId == currentUserId);
    }

    public static bool HasMasterAccess(this IProjectEntity entity, int? currentUserId)
    {
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

    public static void RequestMasterAccess(this IProjectEntity field, int? currentUserId)
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

    public static bool HasPlayerAccess(this Character character, int? currentUserId)
    {
      return character.ApprovedClaim?.PlayerUserId == currentUserId;
    }

    public static bool HasAnyAccess(this Character character, int? currentUserIdOrDefault)
    {
      return character.HasMasterAccess(currentUserIdOrDefault) || character.HasPlayerAccess(currentUserIdOrDefault);
    }

    public static bool HasPlayerAccesToClaim(this Claim claim, int? currentUserIdOrDefault)
    {
      return claim.PlayerUserId == currentUserIdOrDefault;
    }

    public static bool HasEditAccess(this ProjectField projectField, int currentUserId, Character character, Claim claim)
    {
      if (projectField.Project.HasMasterAccess(currentUserId))
      {
        return true;
      }
      switch (projectField.FieldBoundTo)
      {
        case FieldBoundTo.Character:
          return character.HasPlayerAccess(currentUserId) && projectField.CanPlayerEdit;
        case FieldBoundTo.Claim:
          return claim.HasPlayerAccesToClaim(currentUserId) && projectField.CanPlayerEdit;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    public static void RequestEditAccess(this ProjectField projectField, int currentUserId, Character character, Claim claim)
    {
      var editAccess = projectField.HasEditAccess(currentUserId, character, claim);
      if (!editAccess)
      {
        throw new NoAccessToProjectException(character, currentUserId);
      }
    }
  }
}