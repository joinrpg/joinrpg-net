using System;
using System.Linq;
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

    public static void RequestMasterAccess(this IProjectEntity field, int currentUserId, Func<ProjectAcl, bool> requiredAccess)
    {
      if (!field.HasMasterAccess(currentUserId, requiredAccess))
      {
        throw new Exception();
      }
    }

    public static void RequestMasterAccess(this IProjectEntity field, int currentUserId)
    {
      field.RequestMasterAccess(currentUserId, acl => true);
    }
  }
}