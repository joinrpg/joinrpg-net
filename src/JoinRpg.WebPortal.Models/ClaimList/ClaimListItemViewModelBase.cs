using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.ClaimList
{
    public abstract class ClaimListItemViewModelBase : ILinkable
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

        protected ClaimListItemViewModelBase([NotNull]
            Claim claim,
            int currentUserId)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            var accessArguments = new AccessArguments(claim, currentUserId);


            var lastComment = claim.CommentDiscussion.Comments.Where(c => c.IsVisibleToPlayer)
                .OrderByDescending(c => c.CommentId).FirstOrDefault();

            ClaimId = claim.ClaimId;

            ClaimFullStatusView = new ClaimFullStatusView(claim, accessArguments);
            Name = claim.Name;
            Player = claim.Player;

            UpdateDate = lastComment?.LastEditTime ?? claim.CreateDate;
            CreateDate = claim.CreateDate;
            Responsible = claim.ResponsibleMasterUser;
            LastModifiedBy = lastComment?.Author ?? claim.Player;

            ProjectId = claim.ProjectId;
            ProjectName = claim.Project.ProjectName;
        }

        #region Implementation of ILinkable

        public LinkType LinkType => LinkType.Claim;
        public string Identification => ClaimId.ToString();
        int? ILinkable.ProjectId => ProjectId;

        #endregion
    }
}
