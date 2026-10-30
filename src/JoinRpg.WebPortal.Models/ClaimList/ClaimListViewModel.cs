using JoinRpg.DataModel;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Web.Claims;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.Web.Models.Claims;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimListViewModel(
   ICurrentUserAccessor currentUserId,
   IReadOnlyList<ClaimInfo> claimsList,
   Dictionary<CommentDiscussionId, int> unreadComments,
   string title,
   ProjectInfo projectInfo,
   IClaimProblemValidator claimValidator) : IOperationsAwareView
{
    public string InlineTitle { get; private set; } = title;

    bool IOperationsAwareView.ShowCharacterCreateButton => false;

    public IReadOnlyCollection<ClaimListItemViewModel> Items { get; } = [.. claimsList.Select(c => ClaimListBuilder.BuildItem(currentUserId, claimValidator, c, unreadComments))];

    public ProjectIdentification ProjectId { get; } = projectInfo.ProjectId;
    public IReadOnlyCollection<ClaimIdentification> ClaimIds { get; } = [.. claimsList.Select(c => c.ClaimId)];
    public IReadOnlyCollection<CharacterIdentification> CharacterIds { get; } = [.. claimsList.Select(c => c.Character.Id)];

    public string? CountString => CountHelper.DisplayCount(Items.Count, "заявка", "заявки", "заявок");

    int? IOperationsAwareView.ProjectId => ProjectId;
}

public class MyClaimListViewModel(IReadOnlyCollection<Claim> claims, IReadOnlyDictionary<CommentDiscussionId, int> unreadComments)
{
    public IReadOnlyCollection<ItemViewModel> Items { get; } =
        [.. claims.Select(c => new ItemViewModel(c, unreadComments.GetValueOrDefault(new CommentDiscussionId(c.CommentDiscussionId))))];

    public class ItemViewModel
    {
        public int UnreadCommentsCount { get; }

        public ItemViewModel(Claim claim, int unreadCommentsCount)
        {
            UnreadCommentsCount = unreadCommentsCount;

            ArgumentNullException.ThrowIfNull(claim);

            LastCommentByPlayer = (claim.LastPlayerCommentAt ?? claim.CreateDate) > claim.LastVisibleMasterCommentAt;

            ClaimId = claim.ClaimId;

            ClaimFullStatusView = ClaimStatusBuilders.CreateFullStatus(claim, AccessArguments.None);
            Name = claim.Character.CharacterName;

            UpdateDate = claim.LastUpdateDateTime;

            ProjectId = claim.ProjectId;
            ProjectName = claim.Project.ProjectName;
        }

        public bool LastCommentByPlayer { get; }

        [Display(Name = "Имя")]
        public string Name { get; set; }

        [Display(Name = "Игра")]
        public string ProjectName { get; set; }

        [Display(Name = "Статус")]
        public ClaimFullStatusView ClaimFullStatusView { get; set; }

        [Display(Name = "Обновлена"), UIHint("EventTime")]
        public DateTime? UpdateDate { get; set; }

        public int ProjectId { get; }

        public int ClaimId { get; }
    }
}
