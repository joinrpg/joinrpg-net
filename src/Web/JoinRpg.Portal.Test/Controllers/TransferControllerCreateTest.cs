using System.ComponentModel.DataAnnotations;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.Portal.Controllers.Money;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Money;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Мастера в переводе могли не выбрать: биндер на пустое значение молча даёт null без ошибки в ModelState.
/// Это должно быть понятной ошибкой на форме, а не падением в сервисе.
/// </summary>
public class TransferControllerCreateTest
{
    [Fact]
    public async Task EmptySender_ReturnsFormWithError_AndDoesNotCreateTransfer()
    {
        var mock = new MockedProject();
        var financeService = new RecordingFinanceService();
        var controller = CreateController(mock, financeService);
        var viewModel = CreateViewModel(mock);
        viewModel.Sender = null;

        var result = await controller.Create(viewModel);

        _ = result.ShouldBeOfType<ViewResult>().Model.ShouldBeOfType<CreateMoneyTransferViewModel>();
        controller.ModelState[nameof(CreateMoneyTransferViewModel.Sender)].ShouldNotBeNull()
            .Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe("Выберите, от кого перевод");
        financeService.CreateTransferCalls.ShouldBe(0);
    }

    [Fact]
    public async Task EmptyReceiver_ReturnsFormWithError_AndDoesNotCreateTransfer()
    {
        var mock = new MockedProject();
        var financeService = new RecordingFinanceService();
        var controller = CreateController(mock, financeService);
        var viewModel = CreateViewModel(mock);
        viewModel.Receiver = null;

        var result = await controller.Create(viewModel);

        _ = result.ShouldBeOfType<ViewResult>();
        controller.ModelState[nameof(CreateMoneyTransferViewModel.Receiver)].ShouldNotBeNull()
            .Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe("Выберите, кому перевод");
        financeService.CreateTransferCalls.ShouldBe(0);
    }

    [Fact]
    public async Task FilledForm_CreatesTransfer()
    {
        var mock = new MockedProject();
        var financeService = new RecordingFinanceService();
        var controller = CreateController(mock, financeService);

        _ = await controller.Create(CreateViewModel(mock));

        financeService.CreateTransferCalls.ShouldBe(1);
    }

    /// <summary>
    /// Два пустых значения равны, но это «не выбрано» — не «перевод самому себе».
    /// </summary>
    [Fact]
    public void BothEmpty_IsNotReportedAsSelfTransfer()
    {
        var viewModel = new CreateMoneyTransferViewModel { Sender = null, Receiver = null, CommentText = "" };

        viewModel.Validate(new ValidationContext(viewModel)).ShouldBeEmpty();
    }

    [Fact]
    public void SameMaster_IsReportedAsSelfTransfer()
    {
        var viewModel = new CreateMoneyTransferViewModel { Sender = new(2), Receiver = new(2), CommentText = "" };

        viewModel.Validate(new ValidationContext(viewModel)).ShouldHaveSingleItem();
    }

    private static CreateMoneyTransferViewModel CreateViewModel(MockedProject mock)
        => new()
        {
            ProjectId = mock.Project.ProjectId,
            Sender = new UserIdentification(mock.Master.UserId),
            Receiver = new UserIdentification(mock.CreateMaster().UserId),
            Money = 100,
            OperationDate = DateOnly.FromDateTime(DateTime.Today),
            CommentText = "Передал на стрелке",
        };

    private static TransferController CreateController(MockedProject mock, IFinanceService financeService)
        => new(
            new FakeProjectMetadataRepository(mock),
            financeService,
            new FakeCurrentUserAccessor(mock.Master.UserId))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

    private sealed class RecordingFinanceService : IFinanceService
    {
        public int CreateTransferCalls { get; private set; }

        public Task CreateTransfer(CreateTransferRequest request)
        {
            CreateTransferCalls++;
            return Task.CompletedTask;
        }

        public Task FeeAcceptedOperation(FeeAcceptedOperationRequest request) => throw new NotSupportedException();
        public Task TransferPaymentAsync(ClaimPaymentTransferRequest request) => throw new NotSupportedException();
        public Task ChangeFee(ClaimIdentification claimIdentification, int feeValue) => throw new NotSupportedException();
        public Task MarkPreferential(MarkPreferentialRequest request) => throw new NotSupportedException();
        public Task RequestPreferentialFee(MarkMeAsPreferentialFeeOperationRequest request) => throw new NotSupportedException();
        public Task MarkTransfer(ApproveRejectTransferRequest request) => throw new NotSupportedException();
    }
}
