using System.Text.Json;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.Web.Claims.Finance;

namespace JoinRpg.WebPortal.Models.Test;

public class FinanceModerationViewBuilderTest
{
    private readonly MockedProject mock = new();

    private FinanceOperation Operation(FinanceOperationType type, int money, PaymentType? paymentType) => new()
    {
        CommentId = 678,
        ClaimId = 45,
        ProjectId = mock.Project.ProjectId,
        OperationType = type,
        MoneyAmount = money,
        OperationDate = new DateTime(2026, 10, 25, 13, 0, 0),
        PaymentTypeId = paymentType?.PaymentTypeId,
    };

    [Fact]
    public void BuildsSummaryOfPaymentToCash()
    {
        var cash = mock.CreateCashPaymentType(mock.Master);

        var view = Operation(FinanceOperationType.Submit, 500, cash).ToModerationView(mock.ProjectInfo);

        view.OperationId.ShouldBe(new FinanceOperationIdentification(new ClaimIdentification(mock.ProjectInfo.ProjectId, 45), 678));
        view.Title.ShouldBe("Оплата");
        view.Money.ShouldBe(500);
        view.OperationDate.ShouldBe(new DateOnly(2026, 10, 25));
        view.PaymentTypeName.ShouldNotBeNull().ShouldContain("Master");
    }

    [Fact]
    public void PreferentialFeeRequestHasNoPaymentType()
    {
        var view = Operation(FinanceOperationType.PreferentialFeeRequest, 0, paymentType: null).ToModerationView(mock.ProjectInfo);

        view.Title.ShouldBe("Запрос льготы");
        view.PaymentTypeName.ShouldBeNull();
    }

    /// <summary>
    /// Сводка — параметр пререндеренного острова: если она не переживёт JSON, остров молча не отрисуется.
    /// </summary>
    [Fact]
    public void SurvivesJsonRoundTrip()
    {
        var view = Operation(FinanceOperationType.Submit, 500, mock.CreateCashPaymentType(mock.Master)).ToModerationView(mock.ProjectInfo);

        var json = JsonSerializer.Serialize(view, JsonSerializerOptions.Web);

        JsonSerializer.Deserialize<FinanceModerationOperationView>(json, JsonSerializerOptions.Web).ShouldBe(view);
    }

    [Fact]
    public void ProposedPaymentForModerator_HasView()
    {
        var operation = Operation(FinanceOperationType.Submit, 500, mock.CreateCashPaymentType(mock.Master));
        operation.State = FinanceOperationState.Proposed;

        operation.ToModerationViewIfAllowed(mock.ProjectInfo, new UserIdentification(mock.Master.UserId)).ShouldNotBeNull();
        operation.ToModerationViewIfAllowed(mock.ProjectInfo, new UserIdentification(mock.Player.UserId)).ShouldBeNull();
    }

    /// <summary>
    /// В списке «Взносы — на модерации» есть и незавершённые онлайн-оплаты, но подтверждать их мастеру нечего.
    /// </summary>
    [Fact]
    public void ProposedOnlinePayment_HasNoView()
    {
        var operation = Operation(FinanceOperationType.Online, 500, paymentType: null);
        operation.State = FinanceOperationState.Proposed;

        operation.ToModerationViewIfAllowed(mock.ProjectInfo, new UserIdentification(mock.Master.UserId)).ShouldBeNull();
    }
}
