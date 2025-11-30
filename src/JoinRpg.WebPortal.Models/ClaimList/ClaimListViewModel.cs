using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Claims;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.Web.Models.Claims;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimListViewModel : IOperationsAwareView
{
    public string InlineTitle { get; private set; }

    bool IOperationsAwareView.ShowCharacterCreateButton => false;

    public IReadOnlyCollection<ClaimListItemViewModel> Items { get; }

    public ProjectIdentification ProjectId { get; }
    public IReadOnlyCollection<ClaimIdentification> ClaimIds { get; }
    public IReadOnlyCollection<CharacterIdentification> CharacterIds { get; }

    public string? CountString => CountHelper.DisplayCount(Items.Count, "заявка", "заявки", "заявок");

    int? IOperationsAwareView.ProjectId => ProjectId;

    public ClaimListViewModel(
       ICurrentUserAccessor currentUserId,
       IReadOnlyCollection<Claim> claims,
       ProjectIdentification projectId,
       Dictionary<int, int> unreadComments,
       string title,
       ProjectInfo projectInfo,
       IProblemValidator<Claim> claimValidator)
    {
        Items = claims
          .Select(c => ClaimListBuilder.BuildItem(c, currentUserId, projectInfo, claimValidator, unreadComments))
          .ToList();
        ClaimIds = claims.Select(c => c.GetId()).ToArray();
        CharacterIds = claims.Select(c => c.GetCharacterId()).ToArray();
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
