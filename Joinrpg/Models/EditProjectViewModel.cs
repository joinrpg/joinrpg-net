using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class EditProjectViewModel
  {
    public int ProjectId { get; set; }

    [DisplayName("Название проекта"), Required]
    public string ProjectName { get; set; }
    [DisplayName("Правила подачи заявок")]
    public MarkdownString ClaimApplyRules { get; set; }

    [DisplayName("Анонс проекта")]
    public MarkdownString ProjectAnnounce { get; set; }

    [ReadOnly(true)]
    public string OriginalName { get; set;  }
  }
}
