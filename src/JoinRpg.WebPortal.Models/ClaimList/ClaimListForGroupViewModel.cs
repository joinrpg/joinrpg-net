using JoinRpg.DataModel;
using JoinRpg.Domain.Problems;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimListForGroupViewModel : ClaimListViewModel, IOperationsAwareView
{
    public CharacterGroupDetailsViewModel GroupModel { get; }
    public ClaimListForGroupViewModel
        (int currentUserId,
        IReadOnlyCollection<Claim> claims,
        CharacterGroup @group,
        GroupNavigationPage page,
        Dictionary<int, int> unreadComments,
        IProblemValidator<Claim> claimValidator,
        ProjectInfo projectInfo)
        : base(currentUserId, claims, group.ProjectId, unreadComments, claimValidator, projectInfo)
        => GroupModel = new CharacterGroupDetailsViewModel(group, currentUserId, page);

    int? IOperationsAwareView.CharacterGroupId => GroupModel.CharacterGroupId;
}
