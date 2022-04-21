using JoinRpg.DataModel;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimListForGroupViewModel : ClaimListViewModel, IOperationsAwareView
{
    public CharacterGroupDetailsViewModel GroupModel { get; }
    public ClaimListForGroupViewModel
        (int currentUserId, IReadOnlyCollection<Claim> claims, CharacterGroup @group, GroupNavigationPage page, Dictionary<int, int> unreadComments)
        : base(currentUserId, claims, group.ProjectId, unreadComments)
        => GroupModel = new CharacterGroupDetailsViewModel(group, currentUserId, page);

    int? IOperationsAwareView.CharacterGroupId => GroupModel.CharacterGroupId;
}
