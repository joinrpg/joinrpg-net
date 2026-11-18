using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimListForGroupViewModel(ICurrentUserAccessor currentUserId,
    IReadOnlyList<ClaimInfo> claims,
    CharacterGroupFullInfo @group,
    GroupNavigationPage page,
    Dictionary<CommentDiscussionId, int> unreadComments,
    IClaimProblemValidator claimValidator,
    ProjectInfo projectInfo,
    string title) : ClaimListViewModel(currentUserId, claims, unreadComments, title, projectInfo, claimValidator), IOperationsAwareView
{
    public CharacterGroupDetailsViewModel GroupModel { get; } = new CharacterGroupDetailsViewModel(group, projectInfo, currentUserId.UserIdentificationOrDefault, page);

    int? IOperationsAwareView.CharacterGroupId => GroupModel.CharacterGroupId;

    string? IOperationsAwareView.InlineTitle => null;     //Не вливаем заголовок в строку с кнопочками, она внутри контрола управления группами.
}
