using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static class UserExtensions
  {
    public static IEnumerable<Project> GetProjects(this User user, Func<ProjectAcl, bool> predicate)
    {
      return user.ProjectAcls.Where(predicate).Select(acl => acl.Project);
    }
  }
}
