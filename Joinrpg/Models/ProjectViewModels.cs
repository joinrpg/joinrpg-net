using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
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

    [Display(Name = "Разрешить несколько персонажей одному игроку")]
    public bool EnableManyCharacters { get; set; }

    [Display(Name = "Опубликовать сюжет всем", Description = "Cюжет игры будет раскрыт всем для всеобщего просмотра и послужит обмену опытом среди мастеров.")]
    public bool PublishPlot{ get; set; }

    [ReadOnly(true)]
    public string OriginalName { get; set;  }

    public bool Active { get; set; }
  }

  public class CloseProjectViewModel
  {
    public int ProjectId { get; set; }

    [ReadOnly(true)]
    public string OriginalName { get; set; }

    [Display(Name = "Опубликовать сюжет всем", Description = "Cюжет игры будет раскрыт всем для всеобщего просмотра и послужит обмену опытом среди мастеров.")]
    public bool PublishPlot { get; set; }

    public bool IsMaster { get; set; }
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
    public bool IsMaster { get; }
    public bool IsActive { get; }
    public IEnumerable<Claim>  MyClaims {get; }
    public int ClaimCount { get; set; }

    public int ProjectRootGroupId { get; }

    public bool IsRootGroupAccepting { get; }

    public bool PublishPlot { get; }

    public  ProjectListItemViewModel (Project p, int? user)
    {
      ProjectId = p.ProjectId;
      IsMaster = p.HasMasterAccess(user);
      IsActive = p.Active;
      ProjectAnnounce = new MarkdownViewModel(p.Details?.ProjectAnnounce);
      ProjectName = p.ProjectName;
      MyClaims = p.Claims.Where(c => c.PlayerUserId == user);
      ClaimCount = p.Claims.Count(c => c.IsActive);
      IsAcceptingClaims = p.IsAcceptingClaims;
      ProjectRootGroupId = p.RootGroup.CharacterGroupId;
      IsRootGroupAccepting = p.RootGroup.IsAvailable;
      PublishPlot = p.Details?.PublishPlot ?? false;
      ;
    }

    public static IOrderedEnumerable<T> OrderByDisplayPriority<T>(
      IEnumerable<T> collectionToSort,
      Func<T, ProjectListItemViewModel> getProjectFunc)
    {
      return collectionToSort
        .OrderByDescending(p => getProjectFunc(p)?.IsActive)
        .ThenByDescending(p => getProjectFunc(p)?.IsMaster)
        .ThenByDescending(p => getProjectFunc(p)?.ClaimCount);
    }
  }
}
