using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Finances;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Money;
using JoinRpg.Web.UserProfile;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Страница «деньги мастера» (ByMaster) должна учитывать расходы мастера в блоке «Итого»:
/// иначе колонки «Расходы»/«Запланировано» всегда нули, а «Денег на руках» завышено.
/// </summary>
public class MoneyInfoForUserViewModelTest
{
    private readonly MockedProject mock = new();

    private static readonly ProjectIdentification ProjectId = new(1);

    private static ExpenseInfo Expense(
        int masterUserId,
        ExpenseState state,
        int plannedQuantity,
        int plannedPrice,
        int? actualQuantity = null,
        int? actualPrice = null)
        => new(
            new ExpenseIdentification(ProjectId, masterUserId),
            $"Расход {masterUserId}",
            Description: null,
            [],
            plannedQuantity,
            plannedPrice,
            actualQuantity,
            actualPrice,
            SpentAt: null,
            state,
            MasterId: new UserIdentification(masterUserId),
            CreatedById: new UserIdentification(masterUserId));

    private MoneyInfoForUserViewModel BuildViewModel(params ExpenseInfo[] expenses)
        => new(
            transfers: [],
            mock.MasterInfo,
            urlHelper: new FakeUriService(),
            operations: [],
            operationsData: new FinOperationListData(new Dictionary<ClaimIdentification, ClaimWithPlayer>(), new Dictionary<UserIdentification, UserInfoHeader>()),
            payments: [],
            currentUserId: new FakeCurrentUserAccessor(),
            projectInfo: mock.ProjectInfo,
            cardBuilder: new UserProfileDetailsBuilder(new FakeAvatarUriResolver()),
            expenses);

    [Fact]
    public void Balance_CountsMasterExpenses()
    {
        var viewModel = BuildViewModel(Expense(mock.Master.UserId, ExpenseState.Approved, 2, 100, 2, 90));

        viewModel.Balance.ExpensesBalance.ShouldBe(-180);
        viewModel.Balance.Total.ShouldBe(-180);
    }

    [Fact]
    public void Balance_IgnoresOtherMastersExpenses()
    {
        var other = mock.CreateMaster();

        var viewModel = BuildViewModel(Expense(other.UserId, ExpenseState.Approved, 1, 100, 1, 100));

        viewModel.Balance.ExpensesBalance.ShouldBe(0);
        viewModel.Balance.Total.ShouldBe(0);
        viewModel.Balance.AnythingEverHappens().ShouldBeFalse();
    }

    private class FakeUriService : IUriService
    {
        public string Get(ILinkable link) => "";

        public Uri GetUri(ILinkable link) => new("https://example.com/");
    }

    private class FakeCurrentUserAccessor : ICurrentUserAccessor
    {
        public int? UserIdOrDefault => null;

        public UserDisplayName DisplayName => new("Мастер", null);

        public bool IsAdmin => false;

        public AvatarIdentification? Avatar => null;
    }

    private class FakeAvatarUriResolver : IAvatarUriResolver
    {
        public Uri GetAvatarUri(string? cachedUri, string originalUri) => new(originalUri);
    }
}
