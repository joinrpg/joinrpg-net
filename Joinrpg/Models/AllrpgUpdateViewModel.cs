using System.Collections.Generic;
using System.ComponentModel;

namespace JoinRpg.Web.Models
{
  public class AllrpgUpdateViewModel
  {
    public int ProjectId { get; set; }
    [ReadOnly(true)]
    public string ProjectName { get; set; }

    [ReadOnly(true)]
    public string UpdateResult { get; set; } = "";
  }
}
