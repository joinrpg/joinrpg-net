using JoinRpg.DataModel;
using JoinRpg.Domain.Problems;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimListForGroupViewModel(int currentUserId,
    IReadOnlyCollection<Claim> claims,
    CharacterGroup @group,
    GroupNavigationPage page,
    Dictionary<int, int> unreadComments,
    IProblemValidator<Claim> claimValidator,
    ProjectInfo projectInfo,
    string title) : ClaimListViewModel(currentUserId, claims, group.ProjectId, unreadComments, title), IOperationsAwareView
{
    public CharacterGroupDetailsViewModel GroupModel { get; } = new CharacterGroupDetailsViewModel(group, currentUserId, page);

    private readonly IProblemValidator<Claim> claimValidator = claimValidator;
    private readonly ProjectInfo projectInfo = projectInfo;

    int? IOperationsAwareView.CharacterGroupId => GroupModel.CharacterGroupId;

    string? IOperationsAwareView.InlineTitle => null;     //Не вливаем заголовок в строку с кнопочками, она внутри контрола управления группами.

    public override bool ShowUserColumn => true;

    protected override IEnumerable<ClaimProblem> ValidateClaim(Claim c) => claimValidator.Validate(c, projectInfo);
}
