using System.Collections.Generic;
using System.ComponentModel;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Areas.Admin.Models
{
  public class AllrpgIndexViewModel
  {
    [ReadOnly(true)]
    public IEnumerable<Project> Projects { get; set; }
  }
  public class AssociateAllrpgProjectViewModel
  {
    public int ProjectId { get; set; }
    public int AllrpgProjectId { get; set; }

    [ReadOnly(true)]
    public IEnumerable<Project> Projects { get; set; }
  }
}
