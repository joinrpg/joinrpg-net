using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models
{

  /// <summary>
  /// <see cref="ClaimProblemType"/>
  /// </summary>
  public enum ProblemTypeViewModel
  {
    [Display(Name="Нет ответственного мастера")]
    [UsedImplicitly]
    NoResponsibleMaster,
    [Display(Name="Неверный ответственный мастер")]
    [UsedImplicitly]
    InvalidResponsibleMaster,
    [Display(Name="Заявка без ответа")]
    [UsedImplicitly]
    ClaimNeverAnswered,
    [Display(Name="По заявке нет решения")]
    [UsedImplicitly]
    ClaimNoDecision,
    [UsedImplicitly]
    [Display(Name="Персонаж уже занят")]
    ClaimActiveButCharacterHasApprovedClaim,
    [UsedImplicitly]
    [Display(Name = "Взнос на модерации")]
    FinanceModerationRequired,
    [UsedImplicitly]
    [Display(Name = "Есть переплата по взносу")]
    TooManyMoney,
    [UsedImplicitly]
    [Display(Name = "Обсуждение по заявке остановилось")]
    ClaimDiscussionStopped,
    [UsedImplicitly]
    [Display(Name = "Нет персонажа у заявки")]
    NoCharacterOnApprovedClaim,
    [UsedImplicitly]
    [Display(Name = "Взнос уплачен частично")]
    FeePaidPartially
  }

  public class ClaimProblemListItemViewModel : ClaimListItemViewModel
  {
    [Display(Name="Проблема")]
    public ProblemTypeViewModel ProblemType { get; set; }

    public DateTime? ProblemTime { get; set; }

    public static ClaimProblemListItemViewModel FromClaimProblem(ClaimProblem problem, int currentUserId)
    {
      var self = new ClaimProblemListItemViewModel();
      self.Assign(problem.Claim, currentUserId);
      self.ProblemType = (ProblemTypeViewModel) problem.ProblemType;
      self.ProblemTime = problem.ProblemTime;
      return self;
    }
  }

  public class ClaimShortListItemViewModel
  {
    [Display(Name = "Заявка")]
    public string Name { get; set; }

    [Display(Name = "Игрок")]
    public User Player { get; set; }

    public int ClaimId { get; set; }
    public int ProjectId { get; set; }
  }

  public class ClaimListItemViewModel
  {
    [Display(Name="Заявка")]
    public string Name { get; set; }

    [Display(Name = "Игрок")]
    public User Player { get; set; }

    [Display (Name="Игра")]
    public string ProjectName { get; set; }

    [Display (Name="Статус")]
    public Claim.Status ClaimStatus { get; set; }

    [Display (Name ="Обновлена"),UIHint("EventTime")]
    public DateTime? UpdateDate { get; set; }

    [Display (Name = "Ответственный")]
    public User Responsible { get; set; }

    public int ProjectId { get; set; }

    public int ClaimId{ get; set; }

    public int UnreadCommentsCount { get; set; }

    public static ClaimListItemViewModel FromClaim(Claim claim, int currentUserId)
    {
      var viewModel = new ClaimListItemViewModel();
      viewModel.Assign(claim, currentUserId);
      return viewModel;
    }

    protected void Assign(Claim claim, int currentUserId)
    {
      ClaimId = claim.ClaimId;
      ClaimStatus = claim.ClaimStatus;
      Name = claim.Name;
      Player = claim.Player;
      ProjectId = claim.ProjectId;
      ProjectName = claim.Project.ProjectName;
      UpdateDate = claim.LastUpdateDateTime;
      Responsible = claim.ResponsibleMasterUser;
      UnreadCommentsCount =
        claim.Comments.Count(comment => (comment.IsVisibleToPlayer || claim.HasMasterAccess(currentUserId))
                                        && !comment.IsReadByUser(currentUserId));
    }
  }
}
