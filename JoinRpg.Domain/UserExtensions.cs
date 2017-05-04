using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static class UserExtensions
  {
    public static IEnumerable<Project> GetProjects(this User user, Func<ProjectAcl, bool> predicate)
    {
      return user.ProjectAcls.Where(predicate).Select(acl => acl.Project);
    }

    public static AccessReason GetProfileAccess(this User user, User currentUser)
    {
      if (user.UserId == currentUser.UserId)
      {
        return AccessReason.ItsMe;
      }
      if (user.Claims.Any(claim => claim.HasAnyAccess(currentUser.UserId) && claim.ClaimStatus != Claim.Status.AddedByMaster))
      {
        return AccessReason.Master;
      }
      if (user.ProjectAcls.Any(acl => acl.Project.HasMasterAccess(currentUser.UserId)))
      {
        return AccessReason.CoMaster;
      }
      if (currentUser.Auth?.IsAdmin == true)
      {
        return AccessReason.Administrator;
      }
      return AccessReason.NoAccess;
    }

    public enum AccessReason
    {
      NoAccess,
      ItsMe,
      Master,
      CoMaster,
      Administrator
    }
  }
}
