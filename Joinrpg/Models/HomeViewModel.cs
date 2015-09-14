using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class HomeViewModel
  {
    public IEnumerable<Project> ActiveProjects { get; set; } = new List<Project>();

    public IEnumerable<Project> MyProjects { get; set; } = new List<Project>();

    public IEnumerable<Claim> MyClaims { get; set; } = new List<Claim>();
  }
}
