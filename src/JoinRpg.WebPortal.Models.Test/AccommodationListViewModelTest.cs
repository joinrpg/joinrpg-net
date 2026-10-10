using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DataModel.Mocks.Fakes;
using JoinRpg.Web.Models.Accommodation;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Сводка «Поселение» (<c>/rooms</c>) считает оплаченных нерасселённых по доменным снимкам заявок
/// и по плану поселения, а не по EF-графу заявки (ADR022, #5416).
/// </summary>
public class AccommodationListViewModelTest
{
    private const int BaseFee = 1000;
    private const int AccommodationCost = 500;

    private readonly MockedProject mock = new();
    private readonly ProjectAccommodationType tent;

    public AccommodationListViewModelTest()
    {
        mock.Project.ProjectFeeSettings.Add(new ProjectFeeSetting { StartDate = new DateTime(2020, 1, 1), Fee = BaseFee });
        tent = mock.CreateAccommodationType("Палатка", capacity: 4, cost: AccommodationCost);
        mock.ReInitProjectInfo();
    }

    private Claim CreatePaidClaim(string name, int paid)
    {
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter(name), mock.Player);
        claim.FinanceOperations =
        [
            new FinanceOperation
            {
                MoneyAmount = paid,
                State = FinanceOperationState.Approved,
                OperationType = FinanceOperationType.Submit,
            },
        ];
        return claim;
    }

    private async Task<AccommodationListViewModel> Build(params Claim[] claims)
        => new(
            mock.ProjectInfo,
            await new FakeRoomCategoryPlanRepository(mock).GetAllPlans(mock.ProjectInfo.ProjectId),
            claimsWithoutRoomType: [],
            [.. claims.Select(claim => mock.GetCharacterInfo(claim.Character))],
            new FakeCurrentUserAccessor(new UserIdentification(mock.Master.UserId)));

    [Fact]
    public async Task UnsettledPaidCountIncludesAccommodationFee()
    {
        var fullyPaid = CreatePaidClaim("Оплатил всё", BaseFee + AccommodationCost);
        // Взнос покрыт, а проживание — нет: если бы стоимость проживания не входила в расчёт,
        // заявка посчиталась бы оплаченной.
        var paidOnlyBaseFee = CreatePaidClaim("Оплатил только взнос", BaseFee);
        _ = mock.CreateAccommodationRequest(tent, fullyPaid, paidOnlyBaseFee);

        var model = await Build(fullyPaid, paidOnlyBaseFee);

        var row = model.RoomTypes.ShouldHaveSingleItem();
        row.Occupancy.PendingRequests.ShouldBe(2);
        row.Occupancy.PaidCount.ShouldBe(1);
        model.TotalPaid.ShouldBe(1);
        model.TotalAcceptedNotPaid.ShouldBe(1);
    }

    [Fact]
    public async Task SettledClaimsAreNotCountedAsUnsettledPaid()
    {
        var unsettled = CreatePaidClaim("Ждёт комнату", BaseFee + AccommodationCost);
        _ = mock.CreateAccommodationRequest(tent, unsettled);

        var settled = CreatePaidClaim("Уже в комнате", BaseFee + AccommodationCost);
        _ = mock.CreateRoom(mock.CreateAccommodationRequest(tent, settled));

        var model = await Build(unsettled, settled);

        model.RoomTypes.ShouldHaveSingleItem().Occupancy.PaidCount.ShouldBe(1);
    }
}
