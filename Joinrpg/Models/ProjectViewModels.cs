using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models
{
  public class ProjectLinkViewModel
  {
    public int ProjectId { get; set; }
    public string ProjectName { get; set; }
  }
  public abstract class ProjectViewModelBase 
  {
    public int ProjectId { get; set; }

    [DisplayName("Название проекта"), Required]
    public string ProjectName { get; set; }

    [DisplayName("Анонс проекта")]
    public MarkdownViewModel ProjectAnnounce { get; set; }

    [Display(Name = "Заявки открыты?")]
    public bool IsAcceptingClaims { get; set; }
  }
  public class EditProjectViewModel: ProjectViewModelBase
  {
    [DisplayName("Правила подачи заявок")]
    public MarkdownViewModel ClaimApplyRules { get; set; }

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

  public class ProjectListItemViewModel : ProjectViewModelBase
  {
    public bool IsMaster { get; set; }
    public IEnumerable<Claim>  MyClaims {get; set; }
    public int ClaimCount { get; set; }
  }
}
