using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.ClaimList
{

    public class ClaimListItemViewModel : ILinkable
    {
        [Display(Name = "Имя")]
        public string Name { get; set; }

        [Display(Name = "Игрок")]
        public User Player { get; set; }

        [Display(Name = "Игра")]
        public string ProjectName { get; set; }

        [Display(Name = "Статус")]
        public ClaimFullStatusView ClaimFullStatusView { get; set; }

        [Display(Name = "Обновлена"), UIHint("EventTime")]
        public DateTime? UpdateDate { get; set; }

        [Display(Name = "Создана"), UIHint("EventTime")]
        public DateTime? CreateDate { get; set; }

        [Display(Name = "Ответственный")]
        public User? Responsible { get; set; }

        [CanBeNull]
        public User LastModifiedBy { get; set; }

        public int ProjectId { get; }

        public int ClaimId { get; }

        public int UnreadCommentsCount { get; }

        [Display(Name = "Проблема")]
        public ICollection<ProblemViewModel> Problems { get; set; }

        [Display(Name = "Уплачено")]
        public int FeePaid { get; }

        [Display(Name = "Осталось")]
        public int FeeDue { get; }

        [Display(Name = "Итого взнос")]
        public int TotalFee { get; }

        [Display(Name = "Тип поселения")]
        public string? AccomodationType { get; }


        [Display(Name = "Комната")]
        public string? RoomName { get; }

        [Display(Name = "Льготник")]
        public bool PreferentialFeeUser { get; }

        public ClaimListItemViewModel([NotNull]
            Claim claim,
            int currentUserId)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            var lastComment = claim.CommentDiscussion.Comments.Where(c => c.IsVisibleToPlayer)
                .OrderByDescending(c => c.CommentId).FirstOrDefault();

            ClaimId = claim.ClaimId;

            ClaimFullStatusView = new ClaimFullStatusView(claim, new AccessArguments(claim, currentUserId));
            Name = claim.Name;
            Player = claim.Player;

            UpdateDate = lastComment?.LastEditTime ?? claim.CreateDate;
            CreateDate = claim.CreateDate;
            Responsible = claim.ResponsibleMasterUser;
            LastModifiedBy = lastComment?.Author ?? claim.Player;
            UnreadCommentsCount = claim.CommentDiscussion.GetUnreadCount(currentUserId);

            ProjectId = claim.ProjectId;
            ProjectName = claim.Project.ProjectName;

            FeePaid = claim.ClaimBalance();
            FeeDue = claim.ClaimFeeDue();
            TotalFee = claim.ClaimTotalFee();

            PreferentialFeeUser = claim.PreferentialFeeUser;


            AccomodationType = claim.AccommodationRequest?.AccommodationType.Name;
            RoomName = claim.AccommodationRequest?.Accommodation?.Name;
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
