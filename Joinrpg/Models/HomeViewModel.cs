using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class HomeViewModel
  {
    public IEnumerable<Project> ActiveProjects;

    public IEnumerable<Project> MyProjects;

    public IEnumerable<Claim> MyClaims;
  }
}
