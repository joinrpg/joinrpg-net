using System;
using System.ComponentModel.DataAnnotations;
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
        public User Responsible { get; set; }

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


            (DateTime lastModifiedAt, User lastModifiedBy) = GetLastComment(claim, accessArguments);

            ClaimId = claim.ClaimId;

            ClaimFullStatusView = new ClaimFullStatusView(claim, accessArguments);
            Name = claim.Name;
            Player = claim.Player;

            UpdateDate = lastModifiedAt;
            CreateDate = claim.CreateDate;
            Responsible = claim.ResponsibleMasterUser;
            LastModifiedBy = lastModifiedBy;

            ProjectId = claim.ProjectId;
            ProjectName = claim.Project.ProjectName;

            static (DateTime At, User By) GetLastComment(Claim claim, AccessArguments accessArguments)
            {
                var lastComment = (At: claim.CreateDate, By: claim.Player);

                if (claim.LastPlayerCommentAt is not null && claim.LastPlayerCommentAt > lastComment.At)
                {
                    lastComment = (At: claim.LastPlayerCommentAt.Value.DateTime, By: claim.Player);
                }

                if (claim.LastVisibleMasterCommentAt is not null && claim.LastVisibleMasterCommentAt > lastComment.At)
                {
                    lastComment = (At: claim.LastVisibleMasterCommentAt.Value.DateTime, By: claim.LastVisibleMasterCommentBy!);
                }

                if (accessArguments.MasterAccess && claim.LastMasterCommentAt is not null && claim.LastMasterCommentAt > lastComment.At)
                {
                    lastComment = (At: claim.LastMasterCommentAt.Value.DateTime, By: claim.LastMasterCommentBy!);
                }

                return lastComment;
            }
        }

        #region Implementation of ILinkable

        public LinkType LinkType => LinkType.Claim;
        public string Identification => ClaimId.ToString();
        int? ILinkable.ProjectId => ProjectId;

        #endregion
    }
}
