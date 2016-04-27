using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

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
    [Display(Name="Нет заявок")]
    NotSend = int.MaxValue, 
  }

  public class ClaimListItemViewModel
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

    [Display (Name = "Ответственный")]
    public User Responsible { get; set; }

    [CanBeNull]
    public User LastModifiedBy { get; set; }

    [CanBeNull]
    public int? CharacterId { get; set; }

    public int ProjectId { get; set; }

    public int? ClaimId{ get; set; }

    public int UnreadCommentsCount { get; set; }

    [Display(Name = "Проблема")]
    public ICollection<ProblemViewModel> Problems { get; set; }

    [NotNull, ReadOnly(true)]
    public CustomFieldsViewModel Fields { get; set; }

    public static ClaimListItemViewModel FromClaim([NotNull] Claim claim, int currentUserId)
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      var viewModel = new ClaimListItemViewModel();
      viewModel.Assign(claim, currentUserId);
      viewModel.AssignProjectProperties(claim);
      viewModel.Fields = new CustomFieldsViewModel(currentUserId, claim);
      return viewModel;
    }

    public static ClaimListItemViewModel FromCharacter([NotNull] Character character, int currentUserId)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));

      var viewModel = new ClaimListItemViewModel();
      if (character.ApprovedClaim != null)
      {
        viewModel.Assign(character.ApprovedClaim, currentUserId);
      }
      else
      {
        viewModel.ClaimStatus = ClaimStatusView.NotSend;
      }
      viewModel.AssignProjectProperties(character);
      viewModel.Name = character.CharacterName;
      viewModel.CharacterId = character.CharacterId;
      viewModel.Fields = new CustomFieldsViewModel(currentUserId, character);
      return viewModel;
    }

    private void AssignProjectProperties(IProjectEntity character)
    {
      ProjectId = character.ProjectId;
      ProjectName = character.Project.ProjectName;
    }

    private void Assign([NotNull] Claim claim, int currentUserId)
    {
      var lastComment = claim.Comments.Where(c => c.IsVisibleToPlayer).OrderByDescending(c => c.CommentId).FirstOrDefault();

      ClaimId = claim.ClaimId;
      CharacterId = claim.CharacterId;
      ClaimStatus = (ClaimStatusView) claim.ClaimStatus;
      Name = claim.Name;
      Player = claim.Player;
      
      UpdateDate = lastComment?.LastEditTime ?? claim.CreateDate;
      Responsible = claim.ResponsibleMasterUser;
      LastModifiedBy = lastComment?.Author ?? claim.Player;
      UnreadCommentsCount =
        claim.Comments.Count(comment => (comment.IsVisibleToPlayer || claim.HasMasterAccess(currentUserId))
                                        && !comment.IsReadByUser(currentUserId));
    }

    public ClaimListItemViewModel AddProblems(IEnumerable<ClaimProblem> problem)
    {
      Problems =
        problem.Select(p => new ProblemViewModel(p)).ToList();
      return this;
    }
  }
}
