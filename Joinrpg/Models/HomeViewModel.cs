using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class HomeViewModel
  {
    public IEnumerable<Project> ActiveProjects;

    public IEnumerable<Project> MyProjects;
    public int? ProjectIdToJoin { get; set; }
  }
}
