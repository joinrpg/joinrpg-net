using JoinRpg.DataModel;
using JoinRpg.Domain.Problems;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.Models.ClaimList;

public abstract class ClaimListViewModel : IOperationsAwareView
{
    string? IOperationsAwareView.InlineTitle => Title;
    bool IOperationsAwareView.ShowCharacterCreateButton => false;

    public string? Title { get; set; }

    public IEnumerable<ClaimListItemViewModel> Items { get; }

    public int? ProjectId { get; }
    public IReadOnlyCollection<int> ClaimIds { get; }
    public IReadOnlyCollection<int> CharacterIds { get; }

    public bool ShowCount { get; }
    public abstract bool ShowUserColumn { get; }

    public ClaimListViewModel(
       int currentUserId,
       IReadOnlyCollection<Claim> claims,
       int? projectId,
       Dictionary<int, int> unreadComments,
       string? title,
       bool showCount = true)
    {
        Items = claims
          .Select(c =>
            new ClaimListItemViewModel(
                c,
                currentUserId,
                unreadComments.GetValueOrDefault(c.CommentDiscussionId),
                ValidateClaim(c))
            )
          .ToList();
        ClaimIds = claims.Select(c => c.ClaimId).ToArray();
        CharacterIds = claims.Select(c => c.CharacterId).ToArray();
        ProjectId = projectId;
        ShowCount = showCount;
        Title = title;
    }

    protected abstract IEnumerable<ClaimProblem> ValidateClaim(Claim c);
}

public class RegularClaimListViewModel(
   int currentUserId,
   IReadOnlyCollection<Claim> claims,
   int? projectId,
   Dictionary<int, int> unreadComments,
   IProblemValidator<Claim> claimValidator,
   ProjectInfo projectInfo,
   string title
       ) : ClaimListViewModel(currentUserId, claims, projectId, unreadComments, title, showCount: true)
{
    protected override IEnumerable<ClaimProblem> ValidateClaim(Claim c) => claimValidator.Validate(c, projectInfo);

    public override bool ShowUserColumn => true;
}

public class MyClaimListViewModel : ClaimListViewModel
{
    public MyClaimListViewModel(
      int currentUserId,
      IReadOnlyCollection<Claim> claims,
      Dictionary<int, int> unreadComments,
      string? title)
     : base(currentUserId, claims, projectId: null, unreadComments: unreadComments, title: title) { }

    public override bool ShowUserColumn => false;

    protected override IEnumerable<ClaimProblem> ValidateClaim(Claim c) => [];
}
