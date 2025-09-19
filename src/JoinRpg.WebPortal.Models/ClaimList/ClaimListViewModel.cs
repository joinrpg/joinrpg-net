using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain.Problems;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimListViewModel : IOperationsAwareView
{
    public string InlineTitle { get; private set; }

    bool IOperationsAwareView.ShowCharacterCreateButton => false;

    public IReadOnlyCollection<ClaimListItemViewModel> Items { get; }

    public ProjectIdentification ProjectId { get; }
    public IReadOnlyCollection<int> ClaimIds { get; }
    public IReadOnlyCollection<int> CharacterIds { get; }

    public string? CountString => CountHelper.DisplayCount(Items.Count, "заявка", "заявки", "заявок");

    int? IOperationsAwareView.ProjectId => ProjectId;

    public ClaimListViewModel(
       int currentUserId,
       IReadOnlyCollection<Claim> claims,
       ProjectIdentification projectId,
       Dictionary<int, int> unreadComments,
       string title,
       ProjectInfo projectInfo,
       IProblemValidator<Claim> claimValidator)
    {
        Items = claims
          .Select(c =>
            new ClaimListItemViewModel(
                c,
                currentUserId,
                unreadComments.GetValueOrDefault(c.CommentDiscussionId),
                claimValidator.Validate(c, projectInfo))
            )
          .ToList();
        ClaimIds = claims.Select(c => c.ClaimId).ToArray();
        CharacterIds = claims.Select(c => c.CharacterId).ToArray();
        ProjectId = projectId;
        InlineTitle = title;
    }

}

public class MyClaimListViewModel(IReadOnlyCollection<Claim> claims)
{
    public IReadOnlyCollection<ItemViewModel> Items { get; } = claims.Select(c => new ItemViewModel(c, 0)).ToList();

    public class ItemViewModel
    {
        public int UnreadCommentsCount { get; }

        public ItemViewModel(Claim claim, int unreadCommentsCount)
        {
            UnreadCommentsCount = unreadCommentsCount;

            ArgumentNullException.ThrowIfNull(claim);

            LastCommentByPlayer = (claim.LastPlayerCommentAt ?? claim.CreateDate) > claim.LastVisibleMasterCommentAt;

            ClaimId = claim.ClaimId;

            ClaimFullStatusView = new ClaimFullStatusView(claim, AccessArguments.None);
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
