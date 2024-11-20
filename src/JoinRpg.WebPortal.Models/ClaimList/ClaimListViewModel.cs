using JoinRpg.DataModel;
using JoinRpg.Domain.Problems;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimListViewModel : IOperationsAwareView
{
    public IEnumerable<ClaimListItemViewModel> Items { get; }

    public int? ProjectId { get; }
    public IReadOnlyCollection<int> ClaimIds { get; }
    public IReadOnlyCollection<int> CharacterIds { get; }

    public bool ShowCount { get; }
    public bool ShowUserColumn { get; }

    public ClaimListViewModel(
        int currentUserId,
        IReadOnlyCollection<Claim> claims,
        int? projectId,
        Dictionary<int, int> unreadComments,
        IProblemValidator<Claim> claimValidator,
        ProjectInfo projectInfo,
        bool showCount = true,
        bool showUserColumn = true
        )
        : this(currentUserId, claims, projectId, unreadComments, claimValidator, new[] { projectInfo }, showCount, showUserColumn)
    {
    }

    public ClaimListViewModel(
       int currentUserId,
       IReadOnlyCollection<Claim> claims,
       int? projectId,
       Dictionary<int, int> unreadComments,
       IProblemValidator<Claim> claimValidator,
       IReadOnlyCollection<ProjectInfo> projectInfos,
       bool showCount = true,
       bool showUserColumn = true
       )
    {
        Items = claims
          .Select(c =>
            new ClaimListItemViewModel(
                c,
                currentUserId,
                unreadComments.GetValueOrDefault(c.CommentDiscussionId),
                claimValidator.Validate(c, projectInfos.Single(pi => pi.ProjectId.Value == c.ProjectId)))
            )
          .ToList();
        ClaimIds = claims.Select(c => c.ClaimId).ToArray();
        CharacterIds = claims.Select(c => c.CharacterId).ToArray();
        ProjectId = projectId;
        ShowCount = showCount;
        ShowUserColumn = showUserColumn;
    }
}
