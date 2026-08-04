using JoinRpg.DataModel;
using JoinRpg.Domain.Problems;
using JoinRpg.Interfaces;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimListForGroupViewModel(ICurrentUserAccessor currentUserId,
    IReadOnlyCollection<Claim> claims,
    CharacterGroupFullInfo @group,
    GroupNavigationPage page,
    Dictionary<int, int> unreadComments,
    IProblemValidator<Claim> claimValidator,
    ProjectInfo projectInfo,
    string title) : ClaimListViewModel(currentUserId, claims, group.Id.ProjectId, unreadComments, title, projectInfo, claimValidator), IOperationsAwareView
{
    public CharacterGroupDetailsViewModel GroupModel { get; } = new CharacterGroupDetailsViewModel(group, projectInfo, currentUserId.UserIdentificationOrDefault, page);

    int? IOperationsAwareView.CharacterGroupId => GroupModel.CharacterGroupId;

    string? IOperationsAwareView.InlineTitle => null;     //Не вливаем заголовок в строку с кнопочками, она внутри контрола управления группами.
}
