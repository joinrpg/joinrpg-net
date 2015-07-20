using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.DataModel
{
  public class ProjectAcl
  {
    public int ProjectAclId { get; set; }
    public int ProjectId { get; set; }

    public virtual Project Project { get; set; }

    public int UserId { get; set; }

    public virtual  User User { get; set; }

    public bool CanChangeFields { get; set; }

    public static ProjectAcl CreateRootAcl(int userId)
    {
      return new ProjectAcl()
      {
        CanChangeFields = true,
        UserId = userId
      };
    }
  }
}
