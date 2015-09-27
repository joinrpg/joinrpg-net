using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public abstract class ProjectViewModelBase 
  {
    public int ProjectId { get; set; }

    [DisplayName("Название проекта"), Required]
    public string ProjectName { get; set; }

    [DisplayName("Анонс проекта")]
    public MarkdownString ProjectAnnounce { get; set; }
  }
  public class EditProjectViewModel: ProjectViewModelBase
  {
    [DisplayName("Правила подачи заявок")]
    public MarkdownString ClaimApplyRules { get; set; }

    [ReadOnly(true)]
    public string OriginalName { get; set;  }
  }

  public class ProjectDetailsViewModel : ProjectViewModelBase
  {
    [Display(Name="Проект активен?")]
    public bool IsActive { get; set; }
    [Display(Name = "Дата создания")]
    public DateTime CreatedDate { get; set; }
    public IEnumerable<User>  Masters { get; set; }
  }
}
