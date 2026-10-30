using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Восстановление потерянного платежа банка (ADR028): операция и её комментарий с прежним номером,
/// автор — служебный пользователь онлайн-платежей, взнос пересчитывается, уведомление уходит
/// после записи.
/// </summary>
public class LostPaymentsRestoreServiceTest : ClaimServiceTestBase
{
    /// <summary>Дата денег — <c>stateDate</c> банка, а не момент восстановления.</summary>
    private static readonly DateTime OperationDate = new(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Номер ниже счётчика комментариев фейка — то есть действительно потерянный. В бою счётчик
    /// много больше, здесь важно только что номер ниже него.
    /// </summary>
    private const int LostOperationId = 63617;

    private const int Money = 1500;

    private const string BankPaymentId = "1128001119";

    private readonly FakeVirtualUsersService virtualUsers = new();

    private LostPaymentsRestoreService CreateService(bool isAdmin = true)
        => new(
            unitOfWork,
            CreateCurrentUser(isAdmin: isAdmin),
            metadataRepository,
            new FakeClaimInfoRepository(mock),
            virtualUsers,
            new Lazy<IClaimNotificationService>(() => claimNotifications),
            NullLogger<LostPaymentsRestoreService>.Instance);

    private Claim CreateClaim()
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateClaim(character, mock.Player);
        mock.ReInitProjectInfo();
        return claim;
    }

    /// <summary>
    /// Заявка в проекте с расписанием взносов: при полной оплате взнос фиксируется — так эффект
    /// <c>UpdateClaimFeeIfRequired</c> виден по <see cref="Claim.CurrentFee"/>.
    /// </summary>
    private Claim CreateClaimWithFee(int fee)
    {
        mock.Project.ProjectFeeSettings.Add(new ProjectFeeSetting
        {
            StartDate = new DateTime(2026, 1, 1),
            Fee = fee,
            PreferentialFee = fee,
        });
        return CreateClaim();
    }

    private PaymentTypeIdentification CreateOnlinePaymentType(bool isActive = true)
    {
        var online = new PaymentType(PaymentTypeKind.Online, mock.Project.ProjectId, mock.Master.UserId)
        {
            PaymentTypeId = mock.Project.PaymentTypes.Count + 1,
            Project = mock.Project,
            User = mock.Master,
            Operations = [],
            IsActive = isActive,
        };
        mock.Project.PaymentTypes.Add(online);
        mock.ReInitProjectInfo();
        return new PaymentTypeIdentification(ProjectId, online.PaymentTypeId);
    }

    private static LostPaymentToRestore Payment(
        ClaimIdentification claimId,
        PaymentTypeIdentification paymentTypeId,
        LostRefundToRestore? refund = null,
        int operationId = LostOperationId,
        int money = Money)
        => new(claimId, operationId, paymentTypeId, money, OperationDate, BankPaymentId, "за Васю", refund);

    private static LostRefundToRestore Refund(
        ClaimIdentification claimId,
        PaymentTypeIdentification paymentTypeId,
        int refundedOperationId,
        int money = Money)
        => new(claimId, refundedOperationId, paymentTypeId, money, OperationDate, OperationDate, BankPaymentId, "1115846166");

    /// <summary>
    /// Платёж восстанавливается с прежним номером: <c>orderId</c> банка это <c>CommentId</c>, и
    /// только так повторная сверка находит операцию, а будущий возврат обращается в банк с номером,
    /// который банк знает.
    /// </summary>
    [Fact]
    public async Task RestoresTheLostPaymentWithItsFormerNumber()
    {
        var claim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();

        var result = await CreateService().RestorePaymentAsync(Payment(claim.GetId(), paymentType));

        result.Status.ShouldBe(LostOperationRestoreStatus.Restored);
        result.OperationId.ShouldBe(LostOperationId);
        result.Refund.ShouldBeNull();

        var operation = claim.FinanceOperations.ShouldHaveSingleItem();
        operation.CommentId.ShouldBe(LostOperationId);
        operation.OperationType.ShouldBe(FinanceOperationType.Online);
        operation.State.ShouldBe(FinanceOperationState.Approved);
        operation.MoneyAmount.ShouldBe(Money);
        operation.PaymentTypeId.ShouldBe(paymentType.PaymentTypeId);
        operation.OperationDate.ShouldBe(OperationDate);
        operation.RefundedOperationId.ShouldBeNull();
        operation.BankDetails.ShouldNotBeNull();
        operation.BankDetails!.BankOperationKey.ShouldBe(BankPaymentId);
        operation.BankDetails.BankRefundKey.ShouldBeNull();

        SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// Комментарий восстановленной записи: автор — служебный пользователь онлайн-платежей, значок
    /// в тексте, действие пустое (у боевого онлайн-платежа его нет), видимость игроку обязательна.
    /// </summary>
    [Fact]
    public async Task WritesTheCommentAsThePaymentsUserWithTheRestoredBadge()
    {
        var claim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();

        await CreateService().RestorePaymentAsync(Payment(claim.GetId(), paymentType));

        var comment = claim.FinanceOperations.Single().Comment;
        comment.AuthorUserId.ShouldBe(virtualUsers.PaymentsUser.UserId);
        comment.IsCommentByPlayer.ShouldBeTrue();
        comment.IsVisibleToPlayer.ShouldBeTrue();
        comment.ExtraAction.ShouldBeNull();
        // Время комментария — момент денег (OperationDate), а не момент восстановления:
        // так он встаёт в историю обсуждения на своё место.
        comment.CreatedAt.ShouldBe(OperationDate);
        comment.LastEditTime.ShouldBe(OperationDate);
        comment.CommentText.Text.Contents.ShouldBe(
            $"[восстановлено] Онлайн-оплата на сумму {Money:F2}₽ подтверждена.{Environment.NewLine}{Environment.NewLine}за Васю");
    }

    /// <summary>
    /// Возврат, который банк зафиксировал вместе с платежом, создаётся следом: новой операцией
    /// с отрицательной суммой и ссылкой на исходную — у возврата нет своего номера в банке.
    /// </summary>
    [Fact]
    public async Task RestoresTheRefundTogetherWithTheLostPayment()
    {
        var claim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();
        var refund = Refund(claim.GetId(), paymentType, LostOperationId);

        var result = await CreateService().RestorePaymentAsync(Payment(claim.GetId(), paymentType, refund));

        result.Status.ShouldBe(LostOperationRestoreStatus.Restored);
        result.Refund.ShouldNotBeNull();
        result.Refund!.Status.ShouldBe(LostOperationRestoreStatus.Restored);
        result.Refund.OperationId.ShouldNotBe(LostOperationId);

        var operations = claim.FinanceOperations.ToList();
        operations.Count.ShouldBe(2);

        var restoredRefund = operations.Single(o => o.OperationType == FinanceOperationType.Refund);
        restoredRefund.MoneyAmount.ShouldBe(-Money);
        restoredRefund.State.ShouldBe(FinanceOperationState.Approved);
        restoredRefund.RefundedOperationId.ShouldBe(LostOperationId);
        restoredRefund.PaymentTypeId.ShouldBe(paymentType.PaymentTypeId);
        restoredRefund.BankDetails!.BankRefundKey.ShouldBe("1115846166");
        restoredRefund.Comment.CommentText.Text.Contents.ShouldBe(
            $"[восстановлено] Оформлен возврат платежа от {OperationDate:d} на сумму {Money}");

        // Деньги пришли и ушли: на взнос заявки полный возврат не влияет.
        claim.CurrentFee.ShouldBeNull();
    }

    /// <summary>
    /// Сбой вставки возврата после вставки платежа не уносит уже записанный платёж: взнос
    /// пересчитывается на полную сумму (возврат не записан — деньги остались), письмо о платеже
    /// отправляется, а возврат получает отдельный исход <c>Failed</c> с текстом сбоя; повторный
    /// прогон восстановит его как RefundMissing.
    /// </summary>
    [Fact]
    public async Task KeepsThePaymentWhenTheRefundInsertFails()
    {
        var claim = CreateClaimWithFee(Money);
        var paymentType = CreateOnlinePaymentType();
        FakeCharacterAggregateWriteRepository.ThrowOnLostRefundInsert = new InvalidOperationException("Исходная операция не найдена");
        try
        {
            var result = await CreateService().RestorePaymentAsync(
                Payment(claim.GetId(), paymentType, Refund(claim.GetId(), paymentType, LostOperationId)));

            result.Status.ShouldBe(LostOperationRestoreStatus.Restored);
            result.FeeRecalculated.ShouldBeTrue();
            var refund = result.Refund.ShouldNotBeNull();
            refund.Status.ShouldBe(LostOperationRestoreStatus.Failed);
            refund.OperationId.ShouldBeNull();
            refund.FailureMessage.ShouldNotBeNull().ShouldContain("Исходная операция не найдена");
            refund.FeeRecalculated.ShouldBeTrue();

            // Возврат не записан — взнос фиксируется на полную сумму платежа.
            claim.FinanceOperations.Count.ShouldBe(1);
            claim.CurrentFee.ShouldBe(Money);

            // Письмо о платеже ушло, письма о возврате нет.
            var notification = claimNotifications.OnlinePayments.ShouldHaveSingleItem();
            notification.Text.TemplateContents.ShouldBe($"Онлайн-оплата на сумму {Money:F2}₽ вновь отражена в вашей заявке.");
            notification.FinanceOperationId.FinanceOperationId.ShouldBe(LostOperationId);
        }
        finally
        {
            FakeCharacterAggregateWriteRepository.ThrowOnLostRefundInsert = null;
        }
    }

    /// <summary>
    /// Платёж уже есть и проведён, а возврата у нас нет — восстанавливается один возврат.
    /// </summary>
    [Fact]
    public async Task RestoresOnlyTheRefundWhenThePaymentIsAlreadySettled()
    {
        var claim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();

        var result = await CreateService().RestoreRefundAsync(Refund(claim.GetId(), paymentType, LostOperationId));

        result.Status.ShouldBe(LostOperationRestoreStatus.Restored);
        var operation = claim.FinanceOperations.ShouldHaveSingleItem();
        operation.OperationType.ShouldBe(FinanceOperationType.Refund);
        operation.MoneyAmount.ShouldBe(-Money);
        operation.RefundedOperationId.ShouldBe(LostOperationId);
    }

    /// <summary>
    /// Повторный прогон ничего не вставляет: номер уже занят нашей же операцией.
    /// </summary>
    [Fact]
    public async Task RepeatRunRestoresNothing()
    {
        var claim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();
        var service = CreateService();

        _ = await service.RestorePaymentAsync(Payment(claim.GetId(), paymentType));
        var second = await service.RestorePaymentAsync(Payment(claim.GetId(), paymentType));

        second.Status.ShouldBe(LostOperationRestoreStatus.AlreadyRestored);
        second.OperationId.ShouldBe(LostOperationId);
        claim.FinanceOperations.Count.ShouldBe(1);
    }

    /// <summary>
    /// Повтор возврата узнаётся не по номеру (он каждый раз новый), а по уже проведённому возврату
    /// той же операции — тот же критерий, по которому боевой путь отказывает во втором возврате.
    /// </summary>
    [Fact]
    public async Task RepeatRefundRestoresNothing()
    {
        var claim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();
        var service = CreateService();

        _ = await service.RestoreRefundAsync(Refund(claim.GetId(), paymentType, LostOperationId));
        var second = await service.RestoreRefundAsync(Refund(claim.GetId(), paymentType, LostOperationId));

        second.Status.ShouldBe(LostOperationRestoreStatus.AlreadyRestored);
        claim.FinanceOperations.Count.ShouldBe(1);
    }

    /// <summary>
    /// Номер, который не ниже счётчика комментариев, вставлять нельзя: такая вставка сдвинула бы
    /// счётчик. Строка уходит человеку.
    /// </summary>
    [Fact]
    public async Task RefusesANumberThatIsNotBelowTheIdentityCounter()
    {
        var claim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();

        var result = await CreateService().RestorePaymentAsync(
            Payment(claim.GetId(), paymentType, operationId: FakeCharacterAggregateWriteRepository.CommentIdentityCurrent));

        result.Status.ShouldBe(LostOperationRestoreStatus.NumberNotLost);
        claim.FinanceOperations.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Номер занят комментарием чужого обсуждения — это чужой номер, восстанавливать с ним нельзя:
    /// деньги встали бы на непредвиденную заявку.
    /// </summary>
    [Fact]
    public async Task RefusesANumberTakenByAnotherComment()
    {
        var claim = CreateClaim();
        var otherClaim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();
        _ = await WriteRepository.InsertRestoredClaimComment(new RestoredClaimCommentRow(
            otherClaim.GetId(),
            LostOperationId,
            new UserIdentification(mock.Master.UserId),
            IsCommentByPlayer: false,
            IsVisibleToPlayer: true,
            ExtraAction: null,
            OperationDate,
            "просто комментарий"));

        var result = await CreateService().RestorePaymentAsync(Payment(claim.GetId(), paymentType));

        result.Status.ShouldBe(LostOperationRestoreStatus.NumberTaken);
        claim.FinanceOperations.ShouldBeEmpty();
        otherClaim.FinanceOperations.ShouldBeEmpty();
    }

    /// <summary>
    /// DRP вернул комментарий без операции: восстановление дописывает операцию и банковские
    /// реквизиты к существующему комментарию (Attached), его текст не трогает, а взнос
    /// пересчитывается и уведомление уходит, как после обычной вставки.
    /// </summary>
    [Fact]
    public async Task AttachesThePaymentToTheCommentRestoredWithoutIt()
    {
        var claim = CreateClaimWithFee(Money);
        var paymentType = CreateOnlinePaymentType();
        _ = await WriteRepository.InsertRestoredClaimComment(new RestoredClaimCommentRow(
            claim.GetId(),
            LostOperationId,
            new UserIdentification(mock.Master.UserId),
            IsCommentByPlayer: false,
            IsVisibleToPlayer: true,
            ExtraAction: null,
            OperationDate,
            "комментарий, возвращённый DRP"));

        var result = await CreateService().RestorePaymentAsync(Payment(claim.GetId(), paymentType));

        result.Status.ShouldBe(LostOperationRestoreStatus.Attached);
        result.OperationId.ShouldBe(LostOperationId);
        result.Refund.ShouldBeNull();
        result.FeeRecalculated.ShouldBeTrue();
        claim.CurrentFee.ShouldBe(Money);

        var operation = claim.FinanceOperations.ShouldHaveSingleItem();
        operation.CommentId.ShouldBe(LostOperationId);
        operation.OperationType.ShouldBe(FinanceOperationType.Online);
        operation.State.ShouldBe(FinanceOperationState.Approved);
        operation.MoneyAmount.ShouldBe(Money);
        operation.BankDetails.ShouldNotBeNull();
        operation.BankDetails!.BankOperationKey.ShouldBe(BankPaymentId);
        // Текст комментария, возвращённого DRP, не тронут — нового комментария нет.
        operation.Comment.CommentText.Text.Contents.ShouldBe("комментарий, возвращённый DRP");

        var notification = claimNotifications.OnlinePayments.ShouldHaveSingleItem();
        notification.Text.TemplateContents.ShouldBe($"Онлайн-оплата на сумму {Money:F2}₽ вновь отражена в вашей заявке.");
        notification.FinanceOperationId.FinanceOperationId.ShouldBe(LostOperationId);
    }

    /// <summary>
    /// Восстанавливать может только админ сайта: атрибут контроллера — не единственная защита
    /// записи денег.
    /// </summary>
    [Fact]
    public async Task RequiresSiteAdmin()
    {
        var claim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();

        await Should.ThrowAsync<MustBeAdminException>(
            () => CreateService(isAdmin: false).RestorePaymentAsync(Payment(claim.GetId(), paymentType)));

        claim.FinanceOperations.ShouldBeEmpty();
    }

    /// <summary>
    /// Заблокированный проект восстанавливают инструменты DRP, а не этот сервис (ADR023).
    /// </summary>
    [Fact]
    public async Task RefusesABlockedProject()
    {
        var claim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();

        await Should.ThrowAsync<ProjectBlockedException>(
            () => CreateService().RestorePaymentAsync(Payment(claim.GetId(), paymentType)));

        claim.FinanceOperations.ShouldBeEmpty();
    }

    /// <summary>
    /// Платёж фиксирует взнос заявки по цене на день денег, а уведомление уходит только после
    /// записи: откатившаяся операция не должна породить письмо.
    /// </summary>
    [Fact]
    public async Task NotifiesAfterTheRecordIsSaved()
    {
        var claim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();
        var sentAtSaveTime = -1;
        OnSaveChanges = _ => sentAtSaveTime = SentNotifications.Count;

        await CreateService().RestorePaymentAsync(Payment(claim.GetId(), paymentType));

        sentAtSaveTime.ShouldBe(0);
        var notification = claimNotifications.OnlinePayments.ShouldHaveSingleItem();
        notification.Text.TemplateContents.ShouldBe($"Онлайн-оплата на сумму {Money:F2}₽ вновь отражена в вашей заявке.");
        notification.FinanceOperationId.FinanceOperationId.ShouldBe(LostOperationId);
        notification.ClaimId.ShouldBe(claim.GetId());
    }

    [Fact]
    public async Task NotifiesAboutTheRestoredRefundToo()
    {
        var claim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();

        await CreateService().RestorePaymentAsync(
            Payment(claim.GetId(), paymentType, Refund(claim.GetId(), paymentType, LostOperationId)));

        var notifications = claimNotifications.OnlinePayments;
        notifications.Count.ShouldBe(2);
        notifications[1].Text.TemplateContents.ShouldBe($"Возврат на сумму {Money:F2}₽ вновь отражён в вашей заявке.");
    }

    /// <summary>
    /// Письмо — не часть восстановления: операция уже записана, и откатывать её из-за очереди
    /// уведомлений нельзя.
    /// </summary>
    [Fact]
    public async Task KeepsTheRecordWhenTheNotificationFails()
    {
        var claim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();
        claimNotifications.FailOnSend = true;

        var result = await CreateService().RestorePaymentAsync(Payment(claim.GetId(), paymentType));

        result.Status.ShouldBe(LostOperationRestoreStatus.Restored);
        claim.FinanceOperations.Count.ShouldBe(1);
    }

    /// <summary>
    /// Операция вставлена своей транзакцией и уже в базе, поэтому падение пересчёта взноса не
    /// должно ни откатывать её, ни выглядеть как «строка не восстановлена»: иначе повторный прогон
    /// ответил бы «уже восстановлена», и незафиксированный взнос потерялся бы молча.
    /// </summary>
    [Fact]
    public async Task ReportsTheFeeFailureWithoutLosingTheOperation()
    {
        var claim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();
        OnSaveChanges = _ => throw new InvalidOperationException("база недоступна");

        var result = await CreateService().RestorePaymentAsync(Payment(claim.GetId(), paymentType));

        result.Status.ShouldBe(LostOperationRestoreStatus.Restored);
        result.FeeRecalculated.ShouldBeFalse();
        claim.FinanceOperations.Count.ShouldBe(1);
    }

    /// <summary>
    /// Пересчёт взноса положительным исходом: платёж с полным возвратом в остальных тестах
    /// обнуляется сам, поэтому здесь один платёж на полную цену расписания — взнос фиксируется.
    /// </summary>
    [Fact]
    public async Task FixesTheFeeByTheSingleRestoredPayment()
    {
        var claim = CreateClaimWithFee(Money);
        var paymentType = CreateOnlinePaymentType();

        var result = await CreateService().RestorePaymentAsync(Payment(claim.GetId(), paymentType));

        result.Status.ShouldBe(LostOperationRestoreStatus.Restored);
        result.FeeRecalculated.ShouldBeTrue();
        claim.CurrentFee.ShouldBe(Money);
    }

    /// <summary>
    /// Взнос фиксируется по цене на дату денег, а не на сегодняшнюю: расписание с тех пор
    /// подорожало, но восстановленный платёж закрывает старую цену — <c>GetFeeToFix</c> берёт
    /// расписание на дату операции.
    /// </summary>
    [Fact]
    public async Task FixesTheFeeAtThePriceOfTheMoneyDate()
    {
        const int oldPrice = 1000;
        mock.Project.ProjectFeeSettings.Add(new ProjectFeeSetting
        {
            StartDate = new DateTime(2026, 1, 1),
            Fee = oldPrice,
            PreferentialFee = oldPrice,
        });
        mock.Project.ProjectFeeSettings.Add(new ProjectFeeSetting
        {
            StartDate = OperationDate.AddMonths(1),
            Fee = Money,
            PreferentialFee = Money,
        });
        var claim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();

        var result = await CreateService().RestorePaymentAsync(Payment(claim.GetId(), paymentType, money: oldPrice));

        result.Status.ShouldBe(LostOperationRestoreStatus.Restored);
        result.FeeRecalculated.ShouldBeTrue();
        claim.CurrentFee.ShouldBe(oldPrice);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task UpdatesTheLastPlayerCommentOnlyForward(int? daysFromPayment)
    {
        var claim = CreateClaim();
        var paymentType = CreateOnlinePaymentType();
        claim.LastPlayerCommentAt = daysFromPayment is { } days ? OperationDate.AddDays(days) : null;

        await CreateService().RestorePaymentAsync(Payment(claim.GetId(), paymentType));

        claim.LastPlayerCommentAt.ShouldBe(daysFromPayment is > 0 ? OperationDate.AddDays(daysFromPayment.Value) : OperationDate);
    }
}
