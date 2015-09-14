using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{
  public class EditProjectViewModel
  {
    public int ProjectId { get; set; }

    [DisplayName("Название проекта"), Required]
    public string ProjectName { get; set; }
    [DisplayName("Правила подачи заявок"),DataType(DataType.MultilineText)]
    public string ClaimApplyRules { get; set; }

    [DisplayName("Анонс проекта"), DataType(DataType.MultilineText)]
    public string ProjectAnnounce { get; set; }

    [ReadOnly(true)]
    public string OriginalName { get; set;  }
  }
}
