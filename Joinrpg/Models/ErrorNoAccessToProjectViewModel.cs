using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.Web.Models
{
  public class ErrorNoAccessToProjectViewModel
  {
    public string ProjectName { get; set; }
    public int ProjectId { get; set; }
    public string CreatorUserName { get; set; }
    public string CreatorEmail { get; set; }
  }
}
