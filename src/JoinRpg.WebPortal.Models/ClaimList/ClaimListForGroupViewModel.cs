using JoinRpg.DataModel;
using JoinRpg.Domain.Problems;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimListForGroupViewModel(ICurrentUserAccessor currentUserId,
    IReadOnlyCollection<Claim> claims,
    CharacterGroup @group,
    GroupNavigationPage page,
    Dictionary<int, int> unreadComments,
    IProblemValidator<Claim> claimValidator,
    ProjectInfo projectInfo,
    string title) : ClaimListViewModel(currentUserId, claims, new ProjectIdentification(group.ProjectId), unreadComments, title, projectInfo, claimValidator), IOperationsAwareView
{
    public CharacterGroupDetailsViewModel GroupModel { get; } = new CharacterGroupDetailsViewModel(group, currentUserId.UserIdOrDefault, page);

    int? IOperationsAwareView.CharacterGroupId => GroupModel.CharacterGroupId;

    string? IOperationsAwareView.InlineTitle => null;     //Не вливаем заголовок в строку с кнопочками, она внутри контрола управления группами.
}
