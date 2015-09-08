using System.ComponentModel;

namespace JoinRpg.Web.Models
{
  public class ProjectCreateViewModel
  {
    [DisplayName("Название игры")]
    public string ProjectName { get; set; }
  }
}
