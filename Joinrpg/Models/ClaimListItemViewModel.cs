using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models
{

  public class ClaimShortListItemViewModel
  {
    [Display(Name = "Заявка")]
    public string Name { get; }

    [Display(Name = "Игрок")]
    public User Player { get; }

    public int ClaimId { get; }
    public int ProjectId { get;}

    public ClaimShortListItemViewModel (Claim claim)
    {
      ClaimId = claim.ClaimId;
      Name = claim.Name;
      Player = claim.Player;
      ProjectId = claim.ProjectId;
    }
  }

  public enum ClaimStatusView
  {
    [Display(Name = "Подана"), UsedImplicitly]
    AddedByUser,
    [Display(Name = "Предложена"), UsedImplicitly]
    AddedByMaster,
    [Display(Name = "Принята"), UsedImplicitly]
    Approved,
    [Display(Name = "Отозвана"), UsedImplicitly]
    DeclinedByUser,
    [Display(Name = "Отклонена"), UsedImplicitly]
    DeclinedByMaster,
    [Display(Name = "Обсуждается"), UsedImplicitly]
    Discussed,
    [Display(Name = "В листе ожидания"), UsedImplicitly]
    OnHold,
  }

  public class ClaimListViewModel : IOperationsAwareView
  {
    public IEnumerable<ClaimListItemViewModel> Items { get;  }
    
    public int? ProjectId { get; }
    public IReadOnlyCollection<int> ClaimIds { get; }
    public IReadOnlyCollection<int> CharacterIds { get; }

    public bool ShowCount { get; }
    public bool ShowUserColumn { get; }

    public ClaimListViewModel (int currentUserId, IReadOnlyCollection<Claim> claims, int? projectId, bool showCount = true, bool showUserColumn = true)
    {
      Items = claims
        .Select(c => new ClaimListItemViewModel(c, currentUserId).AddProblems(c.GetProblems()))
        .ToList();
      ClaimIds = claims.Select(c => c.ClaimId).ToArray();
      CharacterIds = claims.Select(c => c.CharacterId).WhereNotNull().ToArray();
      ProjectId = projectId;
      ShowCount = showCount;
      ShowUserColumn = showUserColumn;
    }
  }

  public class ClaimListItemViewModel : ILinkable
  {
    [Display(Name="Имя")]
    public string Name { get; set; }

    [Display(Name = "Игрок")]
    public User Player { get; set; }

    [Display (Name="Игра")]
    public string ProjectName { get; set; }

    [Display (Name="Статус")]
    public ClaimStatusView ClaimStatus { get; set; }

    [Display (Name ="Обновлена"),UIHint("EventTime")]
    public DateTime? UpdateDate { get; set; }

    [Display(Name = "Создана"), UIHint("EventTime")]
    public DateTime? CreateDate { get; set; }

    [Display (Name = "Ответственный")]
    public User Responsible { get; set; }

    [CanBeNull]
    public User LastModifiedBy { get; set; }

    public int ProjectId { get; }

    public int ClaimId{ get; }

    public int UnreadCommentsCount { get; }

    [Display(Name = "Проблема")]
    public ICollection<ProblemViewModel> Problems { get; set; }

    [NotNull, ReadOnly(true)]
    public CustomFieldsViewModel Fields { get; }

    [Display(Name= "Уплачено")]
    public int FeePaid { get; }
    [Display(Name = "Осталось")]
    public int FeeDue { get; }

    public ClaimListItemViewModel ([NotNull] Claim claim, int currentUserId)
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      var lastComment = claim.CommentDiscussion.Comments.Where(c => c.IsVisibleToPlayer).OrderByDescending(c => c.CommentId).FirstOrDefault();

      ClaimId = claim.ClaimId;
      ClaimStatus = (ClaimStatusView) claim.ClaimStatus;
      
      Name = claim.Name;
      Player = claim.Player;

      UpdateDate = lastComment?.LastEditTime ?? claim.CreateDate;
      CreateDate = claim.CreateDate;
      Responsible = claim.ResponsibleMasterUser;
      LastModifiedBy = lastComment?.Author ?? claim.Player;
      UnreadCommentsCount = claim.CommentDiscussion.GetUnreadCount(currentUserId);

      ProjectId = claim.ProjectId;
      ProjectName = claim.Project.ProjectName;
      Fields = new CustomFieldsViewModel(currentUserId, claim);
      FeePaid = claim.ClaimBalance();
      FeeDue = claim.ClaimFeeDue();
    }

    public ClaimListItemViewModel AddProblems(IEnumerable<ClaimProblem> problem)
    {
      Problems =
        problem.Select(p => new ProblemViewModel(p)).ToList();
      return this;
    }

    #region Implementation of ILinkable

    public LinkType LinkType => LinkType.Claim;
    public string Identification => ClaimId.ToString();
    int? ILinkable.ProjectId => ProjectId;

    #endregion
  }
}
