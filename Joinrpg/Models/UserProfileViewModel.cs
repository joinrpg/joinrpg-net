using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class UserProfileViewModel
  {
    public string DisplayName { get; set; }
    public string FullName { get; set; }
    public IEnumerable<ProjectAcl> ThisUserProjects { get; set; }

    public IEnumerable<Project> CanGrantAccessProjects { get; set; }
  }
}
