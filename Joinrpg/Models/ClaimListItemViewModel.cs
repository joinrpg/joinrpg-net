using System;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using JoinRpg.DataModel;
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
    ClaimNoDecision
  }

  public class ClaimProblemListItemViewModel : ClaimListItemViewModel
  {
    [Display(Name="Проблема")]
    public ProblemTypeViewModel ProblemType { get; set; }

    public DateTime? ProblemTime { get; set; }

    public static ClaimProblemListItemViewModel FromClaimProblem(ClaimProblem problem)
    {
      var self = new ClaimProblemListItemViewModel();
      self.Assign(problem.Claim);
      self.ProblemType = (ProblemTypeViewModel) problem.ProblemType;
      self.ProblemTime = problem.ProblemTime;
      return self;
    }
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

    public static ClaimListItemViewModel FromClaim(Claim claim)
    {
      var claimListItemViewModel = new ClaimListItemViewModel();
      claimListItemViewModel.Assign(claim);
      return claimListItemViewModel;
    }

    protected void Assign(Claim claim)
    {
      ClaimId = claim.ClaimId;
      ClaimStatus = claim.ClaimStatus;
      Name = claim.Name;
      Player = claim.Player;
      ProjectId = claim.ProjectId;
      ProjectName = claim.Name;
      UpdateDate = claim.StatusChangedDate;
      Responsible = claim.ResponsibleMasterUser;
    }
  }
}
