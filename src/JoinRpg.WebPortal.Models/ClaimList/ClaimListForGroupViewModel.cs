using System.Collections.Generic;
using JoinRpg.DataModel;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models.ClaimList
{
    public class ClaimListForGroupViewModel : ClaimListViewModel, IOperationsAwareView
    {
        public CharacterGroupDetailsViewModel GroupModel { get; }
        public ClaimListForGroupViewModel
            (int currentUserId, IReadOnlyCollection<Claim> claims, CharacterGroup @group, GroupNavigationPage page)
            : base(currentUserId, claims, group.ProjectId)
            => GroupModel = new CharacterGroupDetailsViewModel(group, currentUserId, page);

        int? IOperationsAwareView.CharacterGroupId => GroupModel.CharacterGroupId;
    }
}
