using System.Data.Entity;
using System.Diagnostics;
using System.Text;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Impl.Claims;
using PscbApi;
using PscbApi.Models;

namespace JoinRpg.Services.Impl;

public static class FinanceOperationExtensions
{
    /// <summary>
    /// Creates the order id that is used to identify our payments on the bank side
    /// </summary>

    public static string GetOrderId(this FinanceOperation fo) => GetOrderId(fo.CommentId);

    /// <summary>
    /// Типизированный идентификатор финансовой операции (FinanceOperationId соответствует CommentId)
    /// </summary>
    public static FinanceOperationIdentification GetId(this FinanceOperation fo)
        => new(new ProjectIdentification(fo.ProjectId), fo.ClaimId, fo.CommentId);

    /// <summary>
    /// Creates the order id that is used to identify our payments on the bank side
    /// </summary>

    public static string GetOrderId(this Comment comment) => GetOrderId(comment.CommentId);

    /// <summary>
    /// Creates the order id that is used to identify our payments on the bank side
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>

    public static string GetOrderId(int id) => id.ToString().PadLeft(10, '0');
}

/// <inheritdoc cref="IPaymentsService" />
/// <remarks>
/// <para>
/// <b>Сервис намеренно НЕ переведён на <c>ICharacterPropsService.ChangeClaim</c> (ADR014).</b>
/// Разбор велся метод за методом; итог — ни одна операция онлайн-оплаты не укладывается в контракт
/// «одна операция — одно сохранение» без изменения поведения в денежном контуре. Причины делятся на
/// два класса, и обе фундаментальны, а не «пока не дошли руки».
/// </para>
/// <para>
/// <b>1. Идентификатор заказа в банке — это первичный ключ из нашей БД.</b>
/// <c>FinanceOperationExtensions.GetOrderId</c> строится из <c>CommentId</c>, который появляется
/// только после <c>SaveChanges</c>. Поэтому всякая операция, создающая платёж
/// (<see cref="InitiateClaimPaymentAsync"/>, <see cref="InitiateFastPaymentsSystemMobilePaymentAsync"/>,
/// <see cref="PerformRecurrentPaymentAsync(RecurrentPayment, int?, bool)"/>, <see cref="RefundAsync"/>),
/// обязана сохраниться <b>до</b> обращения к банку, а потом сохраниться ещё раз, чтобы записать
/// ответ банка. Лямбда <c>ChangeClaim</c> исполняется <b>до</b> единственного сохранения — выразить
/// в ней эту последовательность нельзя. Это ровно тот же запрет, по которому не мигрирован
/// <c>FinanceOperationsImpl.TransferPaymentAsync</c>.
/// </para>
/// <para>
/// <b>2. У входящего платёжного колбэка нет пользователя.</b> <c>ClaimPaymentSuccess</c> и
/// <c>ClaimPaymentFail</c> объявлены без <c>[Authorize]</c> и с <c>[IgnoreAntiforgeryToken]</c> —
/// банк возвращает плательщика без нашей сессии. Ночная сверка (<c>UpdatePaymentStatusJob</c>,
/// <c>PerformRecurrentPaymentMidnightJob</c>) идёт под роботом-админом, у которого нет ACL в
/// проекте. <c>ChangeClaim</c> же начинается с <c>currentUserAccessor.UserIdentification</c>
/// (бросает для анонима), грузит инициатора как сущность и разворачивает
/// <c>ClaimAccessRequirement</c>, в котором admin-bypass запрещён намеренно (ADR014 §5). Сегодня
/// эти пути не проверяют доступ вовсе и работать обязаны — отказать банку нельзя. Нужен отдельный
/// системный/анонимный вход в <c>ICharacterPropsService</c>; это поправка к ADR014, а не часть
/// миграции. См. отчёт по PR.
/// </para>
/// <para>
/// <b>Решения по <c>ProjectActiveRequirement</c> зафиксированы в документации каждого метода</b> —
/// это «карта на будущее». Сами проверки активности не вводятся: ADR014 требует вводить запрет
/// вместе с миграцией метода, а не отдельно от неё.
/// </para>
/// <para>
/// От <c>[Obsolete] DbServiceImplBase</c> сервис при этом отвязан: всё, что он оттуда брал
/// (<c>UnitOfWork</c>, <c>CurrentUserId</c>, <c>Now</c>, <c>GetCurrentUser</c>), выражается через
/// собственные зависимости.
/// </para>
/// </remarks>
internal class PaymentsService(
    IUnitOfWork unitOfWork,
    IUriService uriService,
    IBankSecretsProvider bankSecrets,
    ICurrentUserAccessor currentUserAccessor,
    Lazy<IClaimNotificationService> claimNotificationService,
    ILogger<PaymentsService> logger,
    IProjectMetadataRepository projectMetadataRepository,
    CommentHelper commentHelper,
    IHttpClientFactory clientFactory) : IPaymentsService
{
    /// <summary>
    /// Время операции. Зафиксировано на экземпляр сервиса — ровно как это делал
    /// <c>DbServiceImplBase</c>, чтобы отвязка от базового класса не меняла отметок времени.
    /// Сервис транзиентный, поэтому на практике это время запроса.
    /// </summary>
    private DateTime Now { get; } = DateTime.UtcNow;

    /// <summary>
    /// Текущий пользователь. У входящих платёжных путей его может не быть вовсе, поэтому обращаться
    /// к нему можно только там, где вызов заведомо пришёл из <c>[Authorize]</c>-действия.
    /// </summary>
    private int CurrentUserId => currentUserAccessor.UserId;

    private async Task<User> GetCurrentUser()
        => await unitOfWork.GetUsersRepository().GetById(CurrentUserId);

    private readonly Lazy<FastPaymentsSystemApi> _lazyFpsApi = new(() => new FastPaymentsSystemApi(clientFactory));

    private readonly Lazy<string?> _lazyExternalPaymentsSystemPaymentUrlTemplate
        = new(() => bankSecrets.Debug
            ? bankSecrets.BankSystemDebugPaymentUrl
            : bankSecrets.BankSystemPaymentUrl);

    private ApiConfiguration GetApiConfiguration(int projectId, int claimId)
    {
        return new ApiConfiguration
        {
            Debug = bankSecrets.Debug,
            ApiEndpoint = bankSecrets.ApiEndpoint,
            ApiDebugEndpoint = bankSecrets.ApiDebugEndpoint,
            MerchantId = bankSecrets.MerchantId,
            ApiKey = bankSecrets.ApiKey,
            ApiDebugKey = bankSecrets.ApiDebugKey,
            DefaultSuccessUrl = uriService.Get(new PaymentSuccessUrl(projectId, claimId)),
            DefaultFailUrl = uriService.Get(new PaymentFailUrl(projectId, claimId)),
        };
    }

    private BankApi GetApi(int projectId, int claimId)
        => new(clientFactory, GetApiConfiguration(projectId, claimId), logger);

    private async Task<Claim> GetClaimAsync(int projectId, int claimId)
    {
        var claim = await unitOfWork.GetClaimsRepository().GetClaim(new(projectId, claimId));
        return claim ?? throw new JoinRpgEntityNotFoundException(claimId, nameof(Claim));
    }

    // TODO: We have to reimagine how we get payment purpose
    private (string GoodName, string Details) GetPurpose(bool recurrent, string projectName)
    {
        var goodName = recurrent
            ? "Сервисы JoinRpg"
            : projectName;
        var details = recurrent
            ? $"Подписка пользователя на {goodName}"
            : $"Билет (организационный взнос) участника на {goodName}";
        return (goodName, details);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Не мигрирован</b>: по сути это чтение — страница продолжения оплаты. Собственных мутаций
    /// нет; единственное изменение делает <see cref="UpdateFinanceOperationAsync(FinanceOperation, PaymentInfo?)"/>,
    /// когда банк сообщил, что счёт уже оплачен или истёк. Права — «только игрок»
    /// (<c>ClaimAccessRequirement.PlayerOnly</c>).
    /// Активность проекта: <b>AllowInactive</b> — метод по дороге фиксирует ответ банка по уже
    /// начатому платежу, то есть относится к входящему платёжному контуру.
    /// </remarks>
    public async Task<FastPaymentsSystemMobilePaymentContext> GetFastPaymentsSystemMobilePaymentContextAsync(int projectId, int claimId, int operationId, FpsPlatform platform)
    {
        var fo = await LoadFinanceOperationAsync(projectId, claimId, operationId);

        // Checking access rights
        if (fo.Claim.PlayerUserId != CurrentUserId)
        {
            throw new NoAccessToProjectException(fo.Project, CurrentUserId);
        }

        var api = GetApi(projectId, claimId);

        var pi = await api.GetPaymentInfoAsync(fo.GetOrderId());

        bool continuePayment;

        if (pi.Status != PaymentInfoQueryStatus.Success)
        {
            logger.LogError("Failed to get payment {financeOperationId} for claim {claimId} to project {projectId} because {bankError}", fo.CommentId, claimId, projectId, pi.ErrorDescription);
            throw new PaymentException(fo.Project, $"Failed to initiate Fast Payments System mobile payment");
        }

        switch (pi.Payment?.Status)
        {
            case PaymentStatus.AwaitingForPayment:
                // Все хорошо, продолжаем
                continuePayment = true;
                break;
            case PaymentStatus.Expired:
            case PaymentStatus.Paid:
                // Что-то пошло не так, и ожидание либо истекло, либо этот счет уже оплачен. Просто возвращаем «не продолжать»
                continuePayment = false;
                logger.LogInformation("Attempt to continue payment {financeOperationId} for claim {claimId} to project {projectId} whereas payment is {bankPaymentState}",
                    fo.CommentId,
                    claimId,
                    projectId,
                    pi.Payment.Status
                    );
                await UpdateFinanceOperationAsync(fo, pi);
                break;
            default:
                // Непонятный статус, как мы здесь оказались?
                logger.LogError("Attempt to continue payment {financeOperationId} for claim {claimId} to project {projectId} whereas payment state is {bankPaymentState}", fo.CommentId, claimId, projectId, pi.Payment.Status);
                await UpdateFinanceOperationAsync(fo, pi);
                throw new PaymentException(fo.Project, "Unable to continue payment that doesn't awaits payment");

        }

        ICollection<FpsBank>? banks = null;

        if (continuePayment)
        {

            if (fo.BankDetails?.QrCodeMeta is null || fo.BankDetails?.QrCodeLink is null)
            {
                logger.LogError("Attempt to continue payment {financeOperationId} for claim {claimId} to project {projectId} that is not continuable", fo.CommentId, claimId, projectId);
                throw new PaymentException(fo.Project, "Unable to continue payment that doesn't awaits payment");
            }

            if (platform != FpsPlatform.Desktop)
            {
                banks = await _lazyFpsApi.Value.GetFastPaymentsSystemBanks(
                    platform,
                    fo.Claim.Player.Extra?.PhoneNumber ?? fo.Claim.Player.FullName,
                    fo.BankDetails.QrCodeMeta);
            }

        }

        var result = new FastPaymentsSystemMobilePaymentContext(banks)
        {
            Amount = (int)pi.Payment.Amount,
            Details = pi.Payment.Details ?? "",
            ClaimId = claimId,
            ProjectId = projectId,
            OperationId = operationId,
            QrCodeUrl = fo.BankDetails?.QrCodeLink,
            ExpectedPlatform = platform,
            ContinuePayment = continuePayment,
        };

        return result;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Не мигрирован</b>: три сохранения, и они не схлопываются. Комментарий с финоперацией
    /// сохраняется первым, потому что идентификатор заказа в банке — это её <c>CommentId</c>;
    /// подписка (<see cref="RecurrentPayment"/>) сохраняется вторым, потому что ей нужен уже
    /// полученный идентификатор заказа; реквизиты QR-кода приходят от банка и сохраняются третьим.
    /// Лямбда <c>ChangeClaim</c> исполняется до единственного сохранения, поэтому выразить это в ней
    /// нельзя.
    /// Права — «только игрок» (<c>ClaimAccessRequirement.PlayerOnly</c>).
    /// Активность проекта: <b>MustBeActive</b> — это создание нового платежа, а не приём входящего.
    /// </remarks>
    public async Task<FastPaymentsSystemMobilePaymentContext> InitiateFastPaymentsSystemMobilePaymentAsync(ClaimPaymentRequest request, FpsPlatform platform)
    {
        // Loading claim
        var claim = await GetClaimAsync(request.ProjectId, request.ClaimId);

        // Checking access rights
        if (claim.PlayerUserId != CurrentUserId)
        {
            throw new NoAccessToProjectException(claim.Project, CurrentUserId);
        }

        var onlinePaymentType = request.Recurrent
            ? claim.Project.ActivePaymentTypes.SingleOrDefault(pt => pt.TypeKind == PaymentTypeKind.OnlineSubscription)
            : claim.Project.ActivePaymentTypes.SingleOrDefault(pt => pt.TypeKind == PaymentTypeKind.Online);

        if (onlinePaymentType is null)
        {
            throw new OnlinePaymentsNotAvailableException(claim.Project);
        }

        if (request.Recurrent && request.Method != PaymentMethod.FastPaymentsSystem)
        {
            throw new PaymentMethodNotAllowedForRecurrentPaymentsException(claim.Project, request.Method);
        }

        if (request.Money <= 0)
        {
            throw new PaymentException(claim.Project, $"Money amount must be positive integer");
        }

        User user = await GetCurrentUser();

        var purpose = GetPurpose(request.Recurrent, claim.Project.ProjectName);

        var message = new FastPaymentsSystemInvoicingMessage
        {
            RecurrentPayment = request.Recurrent,
            Amount = request.Money,
            Details = purpose.Details,
            CustomerAccount = CurrentUserId.ToString(),
            CustomerEmail = user.Email,
            CustomerPhone = user.Extra?.PhoneNumber,
            CustomerComment = request.CommentText ?? purpose.Details,
            PaymentMethod = PscbPaymentMethod.FastPaymentsSystem,
            SuccessUrl = uriService.Get(new PaymentSuccessUrl(request.ProjectId, request.ClaimId)),
            FailUrl = uriService.Get(new PaymentFailUrl(request.ProjectId, request.ClaimId)),
            ExpirationMinutes = 120, // TODO: Make configurable
            Data = new FastPaymentSystemInvoicingMessageData
            {
                CustomerPhone = user.Extra?.PhoneNumber,
                GetQrCode = false,
                GetQrCodeUrl = true,
                FastPaymentsSystemSubscriptionPurpose = request.Recurrent ? purpose.Details : null,
                FastPaymentsSystemRedirectUrl = null,
                Receipt = new Receipt
                {
                    CompanyEmail = User.OnlinePaymentVirtualUser,
                    TaxSystem = TaxSystemType.SimplifiedIncomeOutcome,
                    Items = new List<ReceiptItem>
                    {
                        new ReceiptItem
                        {
                            ObjectType = PaymentObjectType.Service,
                            PaymentType = ItemPaymentType.FullPayment,
                            Price = request.Money,
                            Quantity = 1,
                            TotalPrice = request.Money,
                            VatType = VatSystemType.None,
                            Name = purpose.Details,
                        }
                    }
                }
            }
        };

        var api = GetApi(request.ProjectId, request.ClaimId);

        Task<Comment> commentTask = AddPaymentCommentAsync(claim, onlinePaymentType, request);

        // Creating request to bank
        var invoice = await api.GetFastPaymentSystemInvoice(
            message,
            getOrderId: async () => (await commentTask).Finance.GetOrderId(),
            getRedirectUrl: async () => uriService.Get(new PaymentUpdateUrl(request.ProjectId, request.ClaimId, (await commentTask).CommentId))
        );

        if (invoice.Status != PaymentInfoQueryStatus.Success)
        {
            logger.LogError("Failed to initiate payment {financeOperationId} for claim {claimId} to project {projectId} because {bankError}", message.OrderId, request.ClaimId, request.ProjectId, invoice.ErrorDescription);
            throw new PaymentException(claim.Project, $"Failed to initiate Fast Payments System mobile payment");
        }

        var comment = await commentTask;
        var fo = comment.Finance;

        fo.BankDetails ??= new FinanceOperationBankDetails();
        fo.BankDetails.BankOperationKey = invoice.Payment.Id;
        fo.BankDetails.QrCodeLink = invoice.Payment.QrCodeImageUrl;
        fo.BankDetails.QrCodeMeta = invoice.Payment.QrCodeUrl;
        await unitOfWork.SaveChangesAsync();

        ICollection<FpsBank>? banks = null;
        if (platform != FpsPlatform.Desktop)
        {
            banks = await _lazyFpsApi.Value.GetFastPaymentsSystemBanks(
                platform,
                claim.Player.Extra?.PhoneNumber ?? claim.Player.FullName,
                invoice.Payment.QrCodeUrl);
        }

        var result = new FastPaymentsSystemMobilePaymentContext(banks)
        {
            Amount = request.Money,
            Details = message.Details,
            ClaimId = request.ClaimId,
            ProjectId = request.ProjectId,
            OperationId = comment.CommentId,
            QrCodeUrl = invoice.Payment.QrCodeImageUrl!,
            ExpectedPlatform = platform,
            ContinuePayment = true,
        };

        return result;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Не мигрирован</b>: дескриптор запроса к банку строится вокруг идентификатора заказа,
    /// которым служит <c>CommentId</c> созданной финоперации, — то есть сохранение обязано
    /// произойти <b>внутри</b> построения запроса (колбэк <c>getOrderId</c>). При
    /// <c>Recurrent: true</c> добавляется второе сохранение — подписке нужен уже известный
    /// идентификатор родительского платежа. Разделять ветки «обычный платёж — через
    /// <c>ChangeClaim</c>, подписка — по-старому» значило бы завести два пути создания платежа;
    /// это хуже, чем не мигрировать.
    /// Права — «только игрок» (<c>ClaimAccessRequirement.PlayerOnly</c>).
    /// Активность проекта: <b>MustBeActive</b>.
    /// </remarks>
    public async Task<ClaimPaymentContext> InitiateClaimPaymentAsync(ClaimPaymentRequest request)
    {
        // Loading claim
        var claim = await GetClaimAsync(request.ProjectId, request.ClaimId);

        if (request.Method == PaymentMethod.FastPaymentsSystem)
        {
            throw new PaymentException(claim.Project, $"The {request.Method} is currently not available");
        }

        // Checking access rights
        if (claim.PlayerUserId != CurrentUserId)
        {
            throw new NoAccessToProjectException(claim.Project, CurrentUserId);
        }

        var onlinePaymentType = request.Recurrent
            ? claim.Project.ActivePaymentTypes.SingleOrDefault(pt => pt.TypeKind == PaymentTypeKind.OnlineSubscription)
            : claim.Project.ActivePaymentTypes.SingleOrDefault(pt => pt.TypeKind == PaymentTypeKind.Online);

        if (onlinePaymentType is null)
        {
            throw new OnlinePaymentsNotAvailableException(claim.Project);
        }

        if (request.Recurrent && request.Method != PaymentMethod.FastPaymentsSystem)
        {
            throw new PaymentMethodNotAllowedForRecurrentPaymentsException(claim.Project, request.Method);
        }

        if (request.Money <= 0)
        {
            throw new PaymentException(claim.Project, $"Money amount must be positive integer");
        }

        User user = await GetCurrentUser();

        var purpose = GetPurpose(request.Recurrent, claim.Project.ProjectName);

        var message = new PaymentMessage
        {
            RecurrentPayment = request.Recurrent,
            Amount = request.Money,
            Details = purpose.Details,
            CustomerAccount = CurrentUserId.ToString(),
            CustomerEmail = user.Email,
            CustomerPhone = user.Extra?.PhoneNumber,
            CustomerComment = request.CommentText ?? purpose.Details,
            PaymentMethod = request.Method switch
            {
                PaymentMethod.BankCard => PscbPaymentMethod.BankCards,
                PaymentMethod.FastPaymentsSystem => PscbPaymentMethod.FastPaymentsSystem,
                _ => throw new NotSupportedException($"Payment method {request.Method} is not supported"),
            },
            SuccessUrl = uriService.Get(new PaymentSuccessUrl(request.ProjectId, request.ClaimId)),
            FailUrl = uriService.Get(new PaymentFailUrl(request.ProjectId, request.ClaimId)),
            Data = new PaymentMessageData
            {
                FastPaymentsSystemSubscriptionPurpose = request.Recurrent ? purpose.Details : null,
                // FastPaymentsSystemRedirectUrl = request.Method == PaymentMethod.FastPaymentsSystem
                //     ? uriService.Get(new PaymentSuccessUrl(request.ProjectId, request.ClaimId))
                //     : null,
                Receipt = new Receipt
                {
                    CompanyEmail = User.OnlinePaymentVirtualUser,
                    TaxSystem = TaxSystemType.SimplifiedIncomeOutcome,
                    Items = new List<ReceiptItem>
                    {
                        new ReceiptItem
                        {
                            ObjectType = PaymentObjectType.Service,
                            PaymentType = ItemPaymentType.FullPayment,
                            Price = request.Money,
                            Quantity = 1,
                            TotalPrice = request.Money,
                            VatType = VatSystemType.None,
                            Name = purpose.Details,
                        }
                    }
                }
            }
        };

        // Creating request to bank
        PaymentRequestDescriptor result = await GetApi(request.ProjectId, request.ClaimId)
            .BuildPaymentRequestAsync(
                message,
                async () => (await AddPaymentCommentAsync(claim, onlinePaymentType, request)).GetOrderId()
            );

        return new ClaimPaymentContext
        {
            Accepted = true,
            RequestDescriptor = result
        };
    }

    /// <summary>
    /// Создаёт комментарий с финоперацией и <b>сразу сохраняет его</b>: идентификатор заказа в банке
    /// — это <c>CommentId</c>, и до сохранения его не существует.
    /// </summary>
    /// <remarks>
    /// Именно это сохранение и делает весь контур онлайн-оплат непереводимым на <c>ChangeClaim</c>
    /// (одна операция — одно сохранение, и оно идёт <b>после</b> лямбды). Ср. <c>ClaimFinanceOperations.AcceptFee</c>:
    /// у ручного приёма взноса идентификатора заказа нет, поэтому он мигрирован.
    /// </remarks>
    private async Task<Comment> AddPaymentCommentAsync(
        Claim claim,
        PaymentType paymentType,
        ClaimPaymentRequest request)
    {
        if (request is { Recurrent: true, Refund: true })
        {
            throw new ArgumentException($"{nameof(ClaimPaymentRequest.Refund)} and {nameof(ClaimPaymentRequest.Recurrent)} flags are not compatible", nameof(request));
        }

        if (request is { Refund: true, FinanceOperationToRefundId: null })
        {
            throw new ArgumentException($"{nameof(ClaimPaymentRequest.FinanceOperationToRefundId)} is required when {nameof(ClaimPaymentRequest.Refund)} is true", nameof(request));
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(claim.GetId().ProjectId);

        var commentText = request.CommentText?.Trim() ?? ""; // Do not remove null-coalescing here! Payment comment is not necessary, but it must not be null to create comment.

        // Мы здесь игнорируем созданное нами уведомление, и потом создаем его отдельно в другом месте. Так повелось.
        var (comment, _) = commentHelper.CreateClaimCommentWithNotification(commentText, claim, projectInfo, commentExtraAction: null, ClaimOperationType.PlayerChange, Now);

        comment.Finance = new FinanceOperation
        {
            OperationType = request.Refund ? FinanceOperationType.Refund : FinanceOperationType.Online,
            RefundedOperationId = request.Refund ? request.FinanceOperationToRefundId : null,
            PaymentTypeId = paymentType.PaymentTypeId,
            MoneyAmount = request.Refund ? -request.Money : request.Money,
            OperationDate = request.OperationDate,
            ProjectId = request.ProjectId,
            ClaimId = request.ClaimId,
            Created = Now,
            Changed = Now,
            State = FinanceOperationState.Proposed,
            RecurrentPaymentId = request.Recurrent ? null : request.FromRecurrentPaymentId,
            ReccurrentPaymentInstanceToken = request.Recurrent ? FinanceOperation.MakeInstanceToken(DateTime.UtcNow) : null,
            BankDetails = new FinanceOperationBankDetails(),
        };
        _ = unitOfWork.GetDbSet<Comment>().Add(comment);
        await unitOfWork.SaveChangesAsync();

        if (request.Recurrent)
        {
            comment.Finance.RecurrentPayment = new RecurrentPayment
            {
                ClaimId = claim.ClaimId,
                ProjectId = claim.ProjectId,
                Status = RecurrentPaymentStatus.Created,
                CreateDate = Now,
                PaymentAmount = request.Money,
                PaymentTypeId = paymentType.PaymentTypeId,
                BankParentPayment = comment.Finance.GetOrderId(),
            };
            unitOfWork.GetDbSet<RecurrentPayment>().Add(comment.Finance.RecurrentPayment);
            await unitOfWork.SaveChangesAsync();
        }

        return comment;
    }

    private async Task<FinanceOperation> LoadFinanceOperationAsync(int projectId, int claimId, int operationId)
    {
        // Loading finance operation
        FinanceOperation fo = await unitOfWork.GetDbSet<FinanceOperation>()
            .Include(e => e.Claim)
            .Include(e => e.RecurrentPayment)
            .Include(e => e.PaymentType)
            .Include(e => e.BankDetails)
            .FirstOrDefaultAsync(e => e.CommentId == operationId);

        if (fo == null)
        {
            throw new JoinRpgEntityNotFoundException(operationId, nameof(FinanceOperation));
        }

        if (fo.ClaimId != claimId)
        {
            throw new JoinRpgEntityNotFoundException(claimId, nameof(Claim));
        }

        if (fo.ProjectId != projectId)
        {
            throw new JoinRpgEntityNotFoundException(projectId, nameof(Project));
        }

        if (fo.PaymentType?.TypeKind.IsOnline() != true || fo.OperationType is not (FinanceOperationType.Online or FinanceOperationType.Refund))
        {
            throw new PaymentException(fo.Project, "Finance operation is not online payment");
        }

        return fo;
    }

    private async Task<FinanceOperation?> LoadLastUnapprovedFinanceOperationAsync(int projectId, int claimId)
    {
        const int pageSize = 5;

        // Грязный чит
        var skip = 0;
        while (true)
        {
            // Берем по пять штук
            var operations = await unitOfWork.GetDbSet<FinanceOperation>()
                .Include(e => e.RecurrentPayment)
                .Include(e => e.PaymentType)
                .Where(e => e.ProjectId == projectId
                            && e.ClaimId == claimId
                            && e.OperationType == FinanceOperationType.Online
                            && e.State == FinanceOperationState.Proposed)
                .OrderByDescending(e => e.Created)
                .Skip(skip)
                .Take(pageSize)
                .ToArrayAsync();
            if (operations.Length == 0)
            {
                return null;
            }

            // Ищем первую подходящую
            var result = operations.FirstOrDefault(e => e.PaymentType?.TypeKind.IsOnline() is true);

            // Если нашли, или загружено меньше страницы — выходим
            if (result is not null || operations.Length < pageSize)
            {
                return result;
            }

            skip += pageSize;
        }
    }

    private void UpdateFinanceOperationStatus(FinanceOperation fo, RefundData? refundData)
    {
        if (fo.Approved)
        {
            return;
        }

        if (refundData is null)
        {
            fo.State = FinanceOperationState.Invalid;
        }
        else if (refundData.Status == RefundStatus.Error)
        {
            fo.State = FinanceOperationState.Declined;
        }
        else if (refundData.Status == RefundStatus.Completed)
        {
            fo.State = FinanceOperationState.Approved;
        }

        fo.Changed = Now;
    }

    private void UpdateFinanceOperationStatus(FinanceOperation fo, PaymentData paymentData)
    {
        switch (paymentData.Status)
        {
            // Do nothing
            case PaymentStatus.New:
            case PaymentStatus.AwaitingForPayment:
            case PaymentStatus.Refunded:
            case PaymentStatus.Hold:
            case PaymentStatus.Undefined:
                break;

            // All ok
            case PaymentStatus.Paid:
                fo.State = FinanceOperationState.Approved;
                fo.Changed = Now;
                break;

            // User didn't do anything on payment page
            case PaymentStatus.Expired:
                fo.State = FinanceOperationState.Expired;
                fo.Changed = Now;
                break;

            // Something went wrong
            case PaymentStatus.Cancelled:
            case PaymentStatus.Rejected:
            case PaymentStatus.Error: // TODO: Probably have to store last error within finance op?
                fo.State = FinanceOperationState.Declined;
                fo.Changed = Now;
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Ядро входящего платёжного контура: спрашивает банк о судьбе операции и записывает ответ.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Единственный метод сервиса, который по форме подошёл бы <c>ChangeClaimAsync</c></b>:
    /// внешний вызов идёт до мутаций, мутации идут подряд, сохранение ровно одно, уведомление
    /// уходит после него. Активность проекта здесь — <b>AllowInactive</b> (ADR014: деньги приходят
    /// после закрытия игры, отказать платёжной системе нельзя).
    /// </para>
    /// <para>
    /// <b>И всё равно не мигрирован — из-за прав, а не из-за активности.</b> Вызывают его три входа,
    /// и ни у одного нет пользователя, которого <c>ChangeClaim</c> потребует:
    /// <list type="bullet">
    /// <item>возврат плательщика от банка (<c>ClaimPaymentSuccess</c>/<c>ClaimPaymentFail</c>) — без
    /// <c>[Authorize]</c>, то есть аноним;</item>
    /// <item>ночная сверка <c>UpdatePaymentStatusJob</c> — под роботом-админом, у которого нет ACL
    /// в проекте, а admin-bypass в claim-путях запрещён намеренно (ADR014 §5);</item>
    /// <item><c>PerformRecurrentPaymentMidnightJob</c> — то же самое.</item>
    /// </list>
    /// Сегодня проверок доступа на этом пути нет вовсе, и это осознанно: иначе банк получит отказ.
    /// Чтобы метод переехал, <c>ICharacterPropsService</c> нужен системный вход без пользователя —
    /// это поправка к ADR014, а не часть миграции.
    /// </para>
    /// </remarks>
    private async Task<FinanceOperationState> UpdateFinanceOperationAsync(FinanceOperation fo, PaymentInfo? paymentInfo)
    {
        if (fo.State != FinanceOperationState.Proposed)
        {
            return fo.State;
        }

        logger.LogInformation("Updating online payment {financeOperationId} for claim {claimId} to project {projectId}", fo.CommentId, fo.ClaimId, fo.ProjectId);

        var orderIdStr = fo.RefundOperation
            ? FinanceOperationExtensions.GetOrderId(fo.RefundedOperationId.GetValueOrDefault())
            : fo.GetOrderId();

        // Preparing recurrent payment object
        RecurrentPayment? recurrentPayment = fo.RecurrentPayment;
        if (recurrentPayment is null && fo.RecurrentPaymentId.HasValue)
        {
            recurrentPayment = await unitOfWork.GetDbSet<RecurrentPayment>().FindAsync(fo.RecurrentPaymentId.Value);
        }

        // If recurrent payment is not in created state or this finance operation is not parent finance operation,
        // we should not do anything with it
        if (recurrentPayment?.Status != RecurrentPaymentStatus.Created
            || !string.Equals(recurrentPayment.BankParentPayment, orderIdStr, StringComparison.OrdinalIgnoreCase))
        {
            recurrentPayment = null;
        }

        // Asking bank
        if (paymentInfo is null)
        {
            var api = GetApi(fo.ProjectId, fo.ClaimId);
            paymentInfo = await api.GetPaymentInfoAsync(orderIdStr);
        }

        Claim? claim = null;
        PaymentNotification paymentNotification = PaymentNotification.None;

        // Updating status
        if (paymentInfo.Status == PaymentInfoQueryStatus.Success)
        {
            // Unknown payment means our object is detached somehow from bank object
            if (paymentInfo.ErrorCode == ApiErrorCode.UnknownPayment)
            {
                logger.LogError("Online payment {financeOperationId} for claim {claimId} to project {projectId} has failed", fo.CommentId, fo.ClaimId, fo.ProjectId);
                fo.State = FinanceOperationState.Declined;
                fo.Changed = Now;
            }
            // We have refund operation that was successfully loaded
            else if (fo.RefundOperation && paymentInfo.ErrorCode is null)
            {
                fo.BankDetails ??= new FinanceOperationBankDetails();
                fo.BankDetails.BankOperationKey = paymentInfo.Payment!.Id;

                // Trying to get specific refund by its id
                var refund = paymentInfo.Payment.Refunds?.FirstOrDefault(rf => string.Equals(rf.Id, fo.BankDetails.BankRefundKey, StringComparison.OrdinalIgnoreCase));

                // Updating operation status. If no refund -- no problem, it makes operation invalid
                UpdateFinanceOperationStatus(fo, refund);

                claim = await GetClaimAsync(fo.ProjectId, fo.ClaimId);

                if (fo.Approved)
                {
                    var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(fo.ProjectId));
                    claim.UpdateClaimFeeIfRequired(Now, projectInfo);
                }

                paymentNotification = PaymentNotification.Refund;
            }
            // We have regular operation that was successfully loaded
            else if (!fo.RefundOperation && paymentInfo.ErrorCode is null)
            {
                fo.BankDetails ??= new FinanceOperationBankDetails();
                fo.BankDetails.BankOperationKey = paymentInfo.Payment!.Id;

                UpdateFinanceOperationStatus(fo, paymentInfo.Payment);
                if (fo.State == FinanceOperationState.Approved)
                {
                    logger.LogInformation("Online payment {financeOperationId} for claim {claimId} to project {projectId} has been successfully performed", fo.CommentId, fo.ClaimId, fo.ProjectId);

                    claim = await GetClaimAsync(fo.ProjectId, fo.ClaimId);
                    var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(fo.ProjectId));

                    if (recurrentPayment is not null)
                    {
                        recurrentPayment.BankRecurrencyToken = paymentInfo.Payment.RecurrentPaymentToken;
                        recurrentPayment.BankParentPayment = orderIdStr;
                        recurrentPayment.Status = RecurrentPaymentStatus.Active;
                    }

                    claim.UpdateClaimFeeIfRequired(Now, projectInfo);

                    paymentNotification = fo.RecurrentPaymentId.HasValue
                        ? PaymentNotification.RecurrentCharge
                        : PaymentNotification.Payment;
                }
            }
        }
        else if (paymentInfo.ErrorCode == ApiErrorCode.UnknownPayment)
        {
            fo.State = FinanceOperationState.Invalid;
            fo.Changed = Now;
        }

        if (paymentInfo.ErrorCode is not null)
        {
            logger.LogError("Error updating payment {financeOperationId} with bank code {bankErrorCode} and details:\n{bankError}", fo.CommentId, paymentInfo.ErrorCode, paymentInfo.ErrorDescription);
        }

        // When recurrent payment is not null and its state is still created, it means we can mark it failed
        if (recurrentPayment?.Status == RecurrentPaymentStatus.Created)
        {
            recurrentPayment.CloseDate = Now;
            recurrentPayment.Status = RecurrentPaymentStatus.Failed;
        }

        if (recurrentPayment?.Status == RecurrentPaymentStatus.Failed)
        {
            logger.LogError("Recurrent payment {recurrentPaymentId} setup for claim {claimId} to project {projectId} has failed", recurrentPayment.RecurrentPaymentId, fo.ClaimId, fo.ProjectId);
        }

        // Saving if status was updated
        if (fo.State != FinanceOperationState.Proposed)
        {
            await unitOfWork.SaveChangesAsync();
        }

        // Sending payment notification when needed
        if (paymentNotification != PaymentNotification.None)
        {
            Debug.Assert(claim is not null);
            await SendPaymentNotification(claim, fo.MoneyAmount, fo.GetId(), paymentNotification);
        }

        return fo.State;
    }

    private enum PaymentNotification
    {
        None,
        Payment,
        RecurrentSetup,
        RecurrentCharge,
        Refund,
    }

    private async Task SendPaymentNotification(Claim claim, int sum, FinanceOperationIdentification financeOperationId, PaymentNotification notification)
    {
        if (notification == PaymentNotification.None)
        {
            return;
        }

        try
        {
            var sb = new StringBuilder();

            // TODO: Localize
            switch (notification)
            {
                case PaymentNotification.Payment:
                    sb.Append($"Онлайн-оплата на сумму {sum:F2}₽ подтверждена.");
                    break;
                case PaymentNotification.RecurrentSetup:
                    sb.AppendLine($"Оформлена ежемесячная подписка на сумму {sum:F2}₽.");
                    sb.AppendLine("Списания будут проводиться автоматически.");
                    sb.AppendLine();
                    sb.Append($"Чтобы отказаться от подписки, перейдите в свою заявку.");
                    break;
                case PaymentNotification.RecurrentCharge:
                    sb.Append($"Списание средств по подписке на сумму {sum:F2}₽ подтверждено.");
                    sb.AppendLine();
                    sb.Append($"Чтобы отказаться от подписки, перейдите в свою заявку.");
                    break;
                case PaymentNotification.Refund:
                    sb.AppendLine($"Проведен возврат на сумму {sum:F2}₽ на использованное средство платежа.");
                    sb.AppendLine();
                    sb.Append("Срок фактического зачисления средств зависит от вашего банка, но как правило не превышает 3 банковских дней.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(notification), notification, "Unknown payment notification");
            }

            var email = new ClaimOnlinePaymentNotification(
                claim.GetId(),
                Player: claim.Player.ToUserInfoHeader(),
                new NotificationEventTemplate(sb.ToString()),
                financeOperationId
            );

            await claimNotificationService.Value.SendNotification(email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while sending payment notification of claim {claimId}", claim.ClaimId);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Входящий платёжный колбэк. Не мигрирован</b> — см.
    /// <see cref="UpdateFinanceOperationAsync(FinanceOperation, PaymentInfo?)"/>: у вызывающего нет
    /// пользователя. Активность проекта: <b>AllowInactive</b>.
    /// </remarks>
    public async Task<FinanceOperationState> UpdateClaimPaymentAsync(int projectId, int claimId, int orderId)
        => await UpdateFinanceOperationAsync(await LoadFinanceOperationAsync(projectId, claimId, orderId), null);

    /// <inheritdoc />
    /// <remarks>
    /// <b>Входящий платёжный колбэк</b> (банк вернул плательщика без разбираемого номера заказа).
    /// <b>Не мигрирован</b> по той же причине. Активность проекта: <b>AllowInactive</b>.
    /// </remarks>
    public async Task UpdateLastClaimPaymentAsync(int projectId, int claimId)
    {
        var fo = await LoadLastUnapprovedFinanceOperationAsync(projectId, claimId);
        if (fo is not null)
        {
            await UpdateFinanceOperationAsync(fo, null);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Ночная сверка по расписанию. Не мигрирован</b>: идёт под роботом-админом без ACL в
    /// проекте. Активность проекта: <b>AllowInactive</b>.
    /// </remarks>
    public async Task<FinanceOperationState> UpdateFinanceOperationAsync(FinanceOperation fo)
        => await UpdateFinanceOperationAsync(unitOfWork.GetDbSet<FinanceOperation>().Attach(fo), paymentInfo: null);

    /// <inheritdoc />
    /// <remarks>
    /// <b>Не мигрирован</b>: два сохранения, между ними обращение к банку, и промежуточное состояние
    /// <see cref="RecurrentPaymentStatus.Cancelling"/> — <b>durable по замыслу</b>. Оно
    /// фиксируется до вызова банка, и если вызов не удался, подписка остаётся в «отменяется», а в UI
    /// (<c>RecurrentPaymentFunctionsViewModel</c>) остаётся кнопка повторить. Схлопывание в одно
    /// сохранение это состояние уничтожит: неудачная отмена не оставит следа.
    /// Права — игрок заявки либо мастер с <c>CanManageMoney</c>; точного
    /// <c>ClaimAccessRequirement</c> под такую пару сегодня нет
    /// (<c>MasterOrPlayer</c> шире, <c>ManageMoney</c> у́же), так что перенос 1:1 потребует нового
    /// требования.
    /// Активность проекта: <b>MustBeActive</b> — отмену подписки инициирует человек из UI.
    /// </remarks>
    public async Task<bool?> CancelRecurrentPaymentAsync(int projectId, int claimId, int recurrentPaymentId)
    {
        logger.LogInformation("Trying to cancel recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId}", recurrentPaymentId, claimId, projectId);

        var recurrentPayment = await unitOfWork.GetDbSet<RecurrentPayment>()
            .Where(rp => rp.ClaimId == claimId && rp.ProjectId == projectId && rp.RecurrentPaymentId == recurrentPaymentId)
            .FirstOrDefaultAsync();

        if (recurrentPayment is null)
        {
            logger.LogError("There is no recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId}", recurrentPaymentId, claimId, projectId);
            throw new JoinRpgEntityNotFoundException(recurrentPaymentId, nameof(RecurrentPayment));
        }

        if (!recurrentPayment.Claim.HasPlayerAccesToClaim(CurrentUserId)
              && !recurrentPayment.HasMasterAccess(currentUserAccessor, Permission.CanManageMoney))
        {
            throw new JoinRpgInvalidUserException();
        }

        if (recurrentPayment.Status is not (RecurrentPaymentStatus.Created or RecurrentPaymentStatus.Active or RecurrentPaymentStatus.Cancelling))
        {
            logger.LogError("It is not possible to cancel recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} because its state {recurrentPaymentState} is not appropriate", recurrentPayment.RecurrentPaymentId, claimId, projectId, recurrentPayment.Status);
            return null; // TODO: Do we need to throw something here?
        }

        if (recurrentPayment.Status == RecurrentPaymentStatus.Created)
        {
            recurrentPayment.Status = RecurrentPaymentStatus.Cancelled;
            recurrentPayment.CloseDate = Now;
        }
        else
        {
            recurrentPayment.Status = RecurrentPaymentStatus.Cancelling;
        }
        await unitOfWork.SaveChangesAsync();

        if (recurrentPayment.Status == RecurrentPaymentStatus.Cancelled)
        {
            logger.LogInformation("Recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} has been successfully cancelled", recurrentPaymentId, claimId, projectId);
            return true;
        }

        var api = GetApi(projectId, claimId);
        var result = await api.CancelFastPaymentSystemRecurrentPayments(recurrentPayment.BankParentPayment, recurrentPayment.BankRecurrencyToken);
        if (result.Status == PaymentInfoQueryStatus.Success)
        {
            recurrentPayment.Status = RecurrentPaymentStatus.Cancelled;
            recurrentPayment.CloseDate = Now;
            await unitOfWork.SaveChangesAsync();

            logger.LogInformation("Recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} has been successfully cancelled", recurrentPaymentId, claimId, projectId);
            return true;
        }

        logger.LogError("Failed to cancel recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} because {bankError}", recurrentPayment.RecurrentPaymentId, claimId, projectId, result.ErrorDescription);
        return false;
    }


    /// <inheritdoc />
    /// <remarks>
    /// <b>Повторяющийся платёж по расписанию. Не мигрирован</b>: при <c>internalCall: true</c>
    /// вызывается из <c>PerformRecurrentPaymentMidnightJob</c> под роботом-админом без ACL в
    /// проекте, а сама операция делает три-четыре сохранения вокруг двух обращений к банку
    /// (см. <see cref="InternalPerformRecurrentPaymentAsync"/>).
    /// Активность проекта: <b>AllowInactive</b> для пути джобы — это списание, инициированное
    /// внешним расписанием, а не человеком; отбор в <see cref="FindRecurrentPaymentsAsync"/> и так
    /// фильтрует по <c>Project.Active</c>.
    /// </remarks>
    public Task<FinanceOperation?> PerformRecurrentPaymentAsync(RecurrentPayment recurrentPayment, int? amount, bool internalCall = false)
    {
        if (recurrentPayment.Claim is null)
        {
            throw new ArgumentException($"The {nameof(recurrentPayment.Claim)} property cannot be null.", nameof(recurrentPayment));
        }
        if (recurrentPayment.Project is null)
        {
            throw new ArgumentException($"The {nameof(recurrentPayment.Project)} property cannot be null.", nameof(recurrentPayment));
        }
        if (recurrentPayment.PaymentType is null)
        {
            throw new ArgumentException($"The {nameof(recurrentPayment.Project)} property cannot be null.", nameof(recurrentPayment));
        }

        return InternalPerformRecurrentPaymentAsync(recurrentPayment, amount, internalCall);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Не мигрирован</b> — та же причина, что у перегрузки по сущности. Этот вход зовёт мастер
    /// из UI (<c>ForceRecurrentPayment</c>), права — <c>CanManageMoney</c>
    /// (<c>ClaimAccessRequirement.ManageMoney</c>).
    /// Активность проекта: <b>MustBeActive</b> — принудительное списание инициирует человек.
    /// </remarks>
    public async Task<FinanceOperation?> PerformRecurrentPaymentAsync(int projectId, int claimId, int recurrentPaymentId, int? amount, bool internalCall = false)
    {
        var recurrentPayment = await unitOfWork.GetDbSet<RecurrentPayment>()
            .Include(rp => rp.Claim)
            .Include(rp => rp.Project)
            .Include(rp => rp.PaymentType)
            .Where(rp => rp.ClaimId == claimId && rp.ProjectId == projectId && rp.RecurrentPaymentId == recurrentPaymentId)
            .FirstOrDefaultAsync();

        if (recurrentPayment is null)
        {
            logger.LogError("There is no recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId}", recurrentPaymentId, claimId, projectId);
            throw new JoinRpgEntityNotFoundException(recurrentPaymentId, nameof(RecurrentPayment));
        }

        return await InternalPerformRecurrentPaymentAsync(recurrentPayment, amount, internalCall);
    }

    private async Task<FinanceOperation?> InternalPerformRecurrentPaymentAsync(RecurrentPayment recurrentPayment, int? amount, bool internalCall = false)
    {
        logger.LogInformation("Trying to perform recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId}", recurrentPayment.RecurrentPaymentId, recurrentPayment.ClaimId, recurrentPayment.ProjectId);

        if (!internalCall)
        {
            // Типизированная перегрузка HasAccess вместо [Obsolete]-перегрузки по int: правила те же
            // (ACL проекта плюс право распоряжаться деньгами), но у анонима теперь получается
            // «доступа нет», а не исключение «требуется авторизация».
            if (!recurrentPayment.Claim.HasAccess(
                    currentUserAccessor.UserIdentificationOrDefault,
                    Permission.CanManageMoney))
            {
                throw new JoinRpgInvalidUserException();
            }
        }

        if (recurrentPayment.Status is not RecurrentPaymentStatus.Active)
        {
            logger.LogError("Recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} state {recurrentPaymentState} is not active", recurrentPayment.RecurrentPaymentId, recurrentPayment.ClaimId, recurrentPayment.ProjectId, recurrentPayment.Status);
            return null; // TODO: Do we need to throw something here?
        }

        var purpose = GetPurpose(true, string.Empty);

        var receipt = new Receipt
        {
            CompanyEmail = User.OnlinePaymentVirtualUser,
            TaxSystem = TaxSystemType.SimplifiedIncomeOutcome,
            Items = new List<ReceiptItem>
            {
                new ReceiptItem
                {
                    ObjectType = PaymentObjectType.Service,
                    PaymentType = ItemPaymentType.FullPayment,
                    Price = amount ?? recurrentPayment.PaymentAmount,
                    Quantity = 1,
                    TotalPrice = amount ?? recurrentPayment.PaymentAmount,
                    VatType = VatSystemType.None,
                    Name = purpose.Details,
                }
            }
        };

        var comment = await AddPaymentCommentAsync(
            recurrentPayment.Claim,
            recurrentPayment.PaymentType,
            new ClaimPaymentRequest
            {
                Money = amount ?? recurrentPayment.PaymentAmount,
                ClaimId = recurrentPayment.ClaimId,
                ProjectId = recurrentPayment.ProjectId,
                PayerId = recurrentPayment.Claim.PlayerUserId,
                OperationDate = Now,
                FromRecurrentPaymentId = recurrentPayment.RecurrentPaymentId,
                CommentText = $"Списание средств по подписке от {recurrentPayment.CreateDate:d}",
            });
        var fo = comment.Finance;

        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Acquiring payment code for payment {financeOperationId} of recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId}", fo.CommentId, recurrentPayment.RecurrentPaymentId, recurrentPayment.ClaimId, recurrentPayment.ProjectId);

        var api = GetApi(recurrentPayment.ProjectId, recurrentPayment.ClaimId);

        // First, we have to acquire QR-code token
        var initResult = await api.SetupFastPaymentSystemRecurrentPayments(
            recurrentPayment.BankParentPayment!,
            recurrentPayment.BankRecurrencyToken!,
            recurrentPayment.PaymentAmount,
            purpose.Details);

        if (initResult.Status == PaymentInfoQueryStatus.Success)
        {
            logger.LogInformation("Successfully acquired payment code for payment {financeOperationId} of recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} has been configured", fo.CommentId, recurrentPayment.RecurrentPaymentId, recurrentPayment.ClaimId, recurrentPayment.ProjectId);
        }
        else
        {
            logger.LogError("Payment {financeOperationId} of recurrent payment {recurrentPaymentId} setup for claim {claimId} to project {projectId} has failed because {bankError}", fo.CommentId, recurrentPayment.RecurrentPaymentId, recurrentPayment.ClaimId, recurrentPayment.ProjectId, initResult.ErrorDescription);
            fo.State = FinanceOperationState.Declined;
            fo.Changed = Now;
            await unitOfWork.SaveChangesAsync();
            return fo;
        }

        // Then, we have to initiate a payment with that QR-code token
        var result = await api.PayRecurrent(
            recurrentPayment.BankParentPayment!,
            comment.Finance.GetOrderId(),
            recurrentPayment.BankRecurrencyToken!,
            initResult.FastPaymentSystemRecurrencyId!,
            receipt);

        if (result.Status == PaymentInfoQueryStatus.Success)
        {
            logger.LogInformation("Payment {financeOperationId} of recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} has been successfully initiated", fo.CommentId, recurrentPayment.RecurrentPaymentId, recurrentPayment.ClaimId, recurrentPayment.ProjectId);
            fo.BankDetails ??= new FinanceOperationBankDetails();
            fo.BankDetails.BankOperationKey = result.Payment!.Id;
            if (result.Payment.Status == PaymentStatus.Done)
            {
                fo.State = FinanceOperationState.Approved;
                fo.Changed = Now;
            }
        }
        else
        {
            logger.LogError("Failed to initiate payment {financeOperationId} of recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} because {bankError}", fo.CommentId, recurrentPayment.RecurrentPaymentId, recurrentPayment.ClaimId, recurrentPayment.ProjectId, result.Error?.Description);
            fo.State = FinanceOperationState.Declined;
            fo.Changed = Now;
        }

        await unitOfWork.SaveChangesAsync();

        return comment.Finance;
    }


    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>Не мигрирован</b>: два сохранения вокруг обращения к банку. Первое создаёт финоперацию
    /// возврата в состоянии <c>Proposed</c> <b>до</b> вызова банка — и это не случайность. Если
    /// вызов упадёт по таймауту, банк мог возврат и принять; сохранённая <c>Proposed</c>-операция
    /// позволяет ночной сверке (<c>UpdatePaymentStatusJob</c>) выяснить исход у банка. Единственное
    /// сохранение <c>ChangeClaim</c> (оно идёт после лямбды, то есть после вызова банка) этот след
    /// потеряет: исключение уйдёт до сохранения, и незавершённого возврата в базе не останется
    /// вовсе. Для денежного контура это строго хуже сегодняшнего поведения.
    /// </para>
    /// <para>
    /// Права: <b>сегодня их тут нет вовсе</b> — <see cref="LoadFinanceOperationAsync"/> доступ не
    /// проверяет, а действие контроллера <c>RefundPayment</c> ограничено только <c>[Authorize]</c>.
    /// При переводе требование должно стать <c>ClaimAccessRequirement.ManageMoney</c>, но это
    /// изменение поведения в денежном контуре, и ему нужен отдельный PR — см. отчёт.
    /// Активность проекта: <b>MustBeActive</b> — возврат оформляет мастер из UI.
    /// </para>
    /// </remarks>
    Task<FinanceOperation> IPaymentsService.RefundAsync(int projectId, int claimId, int operationId)
        => RefundAsync(projectId, claimId, operationId);

    private async Task<FinanceOperation> RefundAsync(int projectId, int claimId, int operationId, bool partial = false, int? amount = null)
    {
        var sourceFo = await LoadFinanceOperationAsync(projectId, claimId, operationId);

        if (sourceFo.OperationType != FinanceOperationType.Online)
        {
            logger.LogError("Finance operation {financeOperationId} is not online operation and can not be refunded", operationId);
            throw new PaymentException(sourceFo.Project, $"Finance operation {operationId} was not made online");
        }

        if (sourceFo.State != FinanceOperationState.Approved)
        {
            logger.LogError("Finance operation {financeOperationId} can not be refunded because it was not approved", operationId);
            throw new PaymentException(sourceFo.Project, $"Finance operation {operationId} was not approved");
        }

        if (partial)
        {
            throw new NotImplementedException("Partial refunds are not implemented yet");
        }

        // We have to check was operation already completely refunded or not
        var refundedFo = await unitOfWork.GetDbSet<FinanceOperation>()
            .Where(fo => fo.RefundedOperationId == sourceFo.CommentId && fo.State == FinanceOperationState.Approved)
            .ToArrayAsync();
        var refundedSum = refundedFo.Sum(fo => fo.MoneyAmount);
        if (refundedSum != 0)
        {
            logger.LogError("Finance operation {financeOperationId} can not be refunded because is already refunded", operationId);
            throw new PaymentException(sourceFo.Project, $"Finance operation {operationId} is already refunded");
        }

        var comment = await AddPaymentCommentAsync(
            sourceFo.Claim,
            sourceFo.PaymentType!,
            new ClaimPaymentRequest
            {
                Refund = true,
                Money = sourceFo.MoneyAmount,
                ClaimId = claimId,
                ProjectId = projectId,
                PayerId = sourceFo.Claim.PlayerUserId,
                OperationDate = Now,
                FinanceOperationToRefundId = sourceFo.CommentId,
                FromRecurrentPaymentId = sourceFo.RecurrentPaymentId,
                CommentText = $"Оформлен возврат платежа от {sourceFo.Created:d} на сумму {sourceFo.MoneyAmount}",
            });

        var api = GetApi(projectId, claimId);
        var result = await api.Refund(sourceFo.GetOrderId(), false, null, null);

        var fo = comment.Finance;
        fo.BankDetails ??= new FinanceOperationBankDetails();

        if (result.Status == PaymentInfoQueryStatus.Success && result.CreatedRefund?.Status is not (null or RefundStatus.Error))
        {
            logger.LogInformation("Refund of payment {financeOperationId} for claim {claimId} to project {projectId} has been successfully initiated", sourceFo.CommentId, claimId, projectId);
            fo.BankDetails.BankRefundKey = result.CreatedRefund.Id;
            if (result.CreatedRefund.Status == RefundStatus.Completed)
            {
                fo.State = FinanceOperationState.Approved;
                fo.Changed = Now;
            }
        }
        else
        {
            logger.LogError("Failed to initiate refund of payment {financeOperationId} for claim {claimId} to project {projectId} because {bankError}", sourceFo.CommentId, claimId, projectId, result.ErrorDescription ?? "unknown problem");
            fo.BankDetails.BankRefundKey = result.CreatedRefund?.Id;
            fo.State = FinanceOperationState.Declined;
            fo.Changed = Now;
        }
        await unitOfWork.SaveChangesAsync();

        if (comment.Finance.Approved)
        {
            await SendPaymentNotification(sourceFo.Claim, sourceFo.MoneyAmount, fo.GetId(), PaymentNotification.Refund);
        }

        return comment.Finance;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Ни заявка, ни проект</b>: чистое форматирование строки по настройкам банка, без БД и без
    /// прав. Ни одному props-сервису не принадлежит.
    /// </remarks>
    public string? GetExternalPaymentUrl(string? externalPaymentKey)
        => string.IsNullOrWhiteSpace(externalPaymentKey) || string.IsNullOrWhiteSpace(_lazyExternalPaymentsSystemPaymentUrlTemplate.Value)
            ? null
            : string.Format(_lazyExternalPaymentsSystemPaymentUrlTemplate.Value, externalPaymentKey);

    /// <inheritdoc />
    /// <remarks>
    /// <b>Чтение, а не мутация</b>: постраничный отбор по всем проектам сразу, заявки у операции нет.
    /// Кандидат на переезд в репозиторий, но не в props-сервис.
    /// </remarks>
    public async Task<IReadOnlyList<RecurrentPayment>> FindRecurrentPaymentsAsync(
        int? afterId = null,
        bool? activityStatus = true,
        int pageSize = 100)
    {
        if (pageSize is <= 0 or > 10000)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 10000");
        }

        afterId ??= 0;

        var query = unitOfWork.GetDbSet<RecurrentPayment>()
            .OrderBy(rp => rp.RecurrentPaymentId)
            .Take(pageSize)
            .Where(rp => rp.RecurrentPaymentId > afterId);
        switch (activityStatus)
        {
            case true:
                query = query.Where(
                    rp => rp.Status == RecurrentPaymentStatus.Active
                          && rp.Project.Active
                          && (rp.Claim.ClaimStatus == ClaimStatus.Approved || rp.Claim.ClaimStatus == ClaimStatus.CheckedIn)
                          && rp.PaymentType.IsActive);
                break;
            case false:
                query = query.Where(
                    rp => rp.Status != RecurrentPaymentStatus.Active
                          || !rp.PaymentType.IsActive
                          || !(rp.Claim.ClaimStatus == ClaimStatus.Approved || rp.Claim.ClaimStatus == ClaimStatus.CheckedIn)
                          || !rp.Project.Active);
                break;
        }

        var result = await query.ToArrayAsync();
        return result;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Чтение, а не мутация</b> — то же, что у <see cref="FindRecurrentPaymentsAsync"/>.
    /// </remarks>
    public async Task<IReadOnlyList<FinanceOperation>> FindOperationsOfRecurrentPaymentAsync(
        int recurrentPaymentId,
        DateTime? forPeriod = null,
        IReadOnlySet<FinanceOperationState>? ofStates = null,
        int? afterId = null,
        int pageSize = 100)
    {
        if (pageSize is <= 0 or > 10000)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 10000");
        }

        afterId ??= 0;

        if (ofStates?.Count == 0)
        {
            ofStates = null;
        }

        var query = unitOfWork.GetDbSet<FinanceOperation>()
            .OrderBy(fo => fo.CommentId)
            .Take(pageSize)
            .Where(fo => fo.CommentId > afterId)
            .Where(fo => fo.RecurrentPaymentId == recurrentPaymentId)
            .Where(fo => fo.State == FinanceOperationState.Approved || fo.State == FinanceOperationState.Proposed);
        if (forPeriod.HasValue)
        {
            var instanceToken = FinanceOperation.MakeInstanceToken(forPeriod.Value);
            query = query.Where(fo => fo.ReccurrentPaymentInstanceToken == instanceToken);
        }

        if (ofStates?.Count == 1)
        {
            var state = ofStates.First();
            query = query.Where(fo => fo.State == state);
        }
        else if (ofStates?.Count > 1)
        {
            query = query.Where(fo => ofStates.Contains(fo.State));
        }

        var result = await query.ToArrayAsync();
        return result;
    }

    private abstract class PaymentRedirectUrl : ILinkableClaim
    {
        /// <inheritdoc />
        public LinkType LinkType { get; }

        /// <inheritdoc />
        public string Identification => string.Empty;

        /// <inheritdoc />
        public int? ProjectId { get; }

        /// <inheritdoc />
        public int ClaimId { get; }

        protected PaymentRedirectUrl(LinkType linkType, int projectId, int claimId)
        {
            LinkType = linkType;
            ProjectId = projectId;
            ClaimId = claimId;
        }
    }

    private class PaymentSuccessUrl : PaymentRedirectUrl
    {
        public PaymentSuccessUrl(int projectId, int claimId)
            : base(LinkType.PaymentSuccess, projectId, claimId)
        { }
    }

    private class PaymentFailUrl : PaymentRedirectUrl
    {
        public PaymentFailUrl(int projectId, int claimId)
            : base(LinkType.PaymentFail, projectId, claimId)
        { }
    }

    private class PaymentUpdateUrl : PaymentRedirectUrl, ILinkablePayment
    {
        public int OperationId { get; }

        public PaymentUpdateUrl(int projectId, int claimId, int operationId)
            : base(LinkType.PaymentUpdate, projectId, claimId)
        {
            OperationId = operationId;
        }
    }
}
