using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class ErrorNoAccessToProjectViewModel
  {
    public string ProjectName { get; set; }
    public int ProjectId { get; set; }
    public IEnumerable<User> CanGrantAccess { get; set; }
  }
}
