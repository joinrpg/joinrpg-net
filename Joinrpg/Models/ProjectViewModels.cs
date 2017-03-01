using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;

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
    public IHtmlString ProjectAnnounce { get; set; }

    [Display(Name = "Заявки открыты?")]
    public bool IsAcceptingClaims { get; set; }
  }

  public class EditProjectViewModel
  {
    public int ProjectId { get; set; }

    [DisplayName("Название проекта"), Required]
    public string ProjectName { get; set; }

    [DisplayName("Анонс проекта"), UIHint("MarkdownString")]
    public string ProjectAnnounce { get; set; }

    [Display(Name = "Заявки открыты?")]
    public bool IsAcceptingClaims { get; set; }

    [DisplayName("Правила подачи заявок"), UIHint("MarkdownString")]
    public string ClaimApplyRules { get; set; }

    [Display(Name = "Проверять, что игрок играет только одного персонажа", Description = "Если эта опция включена, при принятии заявки какого-то игрока на одну роль все другие заявки этого игрока будут автоматически отклонены.")]
    public bool StrictlyOneCharacter { get; set; }

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
    public bool IsActive { get; }
    [Display(Name = "Дата создания")]
    public DateTime CreatedDate { get; }
    public IEnumerable<User> Masters { get; }

    public ProjectDetailsViewModel (Project project)
    {
      ProjectAnnounce = project.Details?.ProjectAnnounce.ToHtmlString();
      ProjectId = project.ProjectId;
      ProjectName = project.ProjectName;
      IsActive = project.Active;
      IsAcceptingClaims = project.IsAcceptingClaims;
      CreatedDate = project.CreatedDate;
      Masters = project.ProjectAcls.Select(acl => acl.User);
    }
  }

  public class ProjectListItemViewModel : ProjectViewModelBase
  {
    public bool IsMaster { get; }
    public bool IsActive { get; }
    public IEnumerable<Claim>  MyClaims {get; }
    public int ClaimCount { get; }

    public int ProjectRootGroupId { get; }

    public bool IsRootGroupAccepting { get; }

    public bool PublishPlot { get; }

    public  ProjectListItemViewModel (Project p, int? user)
    {
      ProjectId = p.ProjectId;
      IsMaster = p.HasMasterAccess(user);
      IsActive = p.Active;
      ProjectAnnounce = p.Details?.ProjectAnnounce.ToHtmlString();
      ProjectName = p.ProjectName;
      MyClaims = p.Claims.Where(c => c.PlayerUserId == user);
      ClaimCount = p.Claims.Count(c => c.IsActive);
      IsAcceptingClaims = p.IsAcceptingClaims;
      ProjectRootGroupId = p.RootGroup.CharacterGroupId;
      IsRootGroupAccepting = p.RootGroup.IsAvailable;
      PublishPlot = p.Details?.PublishPlot ?? false;
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
