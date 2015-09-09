using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class UserProfileViewModel
  {
    public string DisplayName { get; set; }
    [DisplayName("ФИО")]
    public string FullName { get; set; }
    public IEnumerable<ProjectAcl> ThisUserProjects { get; set; }

    public IEnumerable<Project> CanGrantAccessProjects { get; set; }
    public int UserId { get; set; }

    public IEnumerable<Project> ProjectsToAdd
      => CanGrantAccessProjects.Where(acl => ThisUserProjects.All(acl1 => acl1.ProjectId != acl.ProjectId));
  }
}
