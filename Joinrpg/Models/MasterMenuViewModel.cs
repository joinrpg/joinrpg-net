using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class MasterMenuViewModel
  {
    public int ProjectId { get; set; }
    public string ProjectName { get; set; }

    public bool HasAllrpg { get; set; }

    public IEnumerable<User> Masters { get; set; }
  }
}
