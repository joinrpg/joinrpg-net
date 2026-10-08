using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Impl.Accommodation;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Сервис заявок. После миграции на <see cref="ICharacterPropsService"/> (ADR014) базового класса у
/// него нет: почти всё делается внутри <c>ChangeClaim</c>, а трём немигрированным методам хватает
/// репозиториев, взятых напрямую из <see cref="IUnitOfWork"/>.
/// </summary>
internal class ClaimServiceImpl(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository,
    IClaimProblemValidator claimValidator,
    ILogger<CharacterServiceImpl> logger,
    ICharacterPropsService characterPropsService,
    IImpersonateAccessor impersonateAccessor,
    UserFieldValidator userFieldValidator,
    IFieldSetupService fieldSetupService
    ) : IClaimService
{
    // Репозитории берутся из UnitOfWork, а не из DI: MyDbContext транзиентен, и DI-экземпляр
    // работал бы с другим контекстом, чем SaveChangesAsync этого сервиса.
    private readonly Lazy<IUserRepository> userRepository = new(unitOfWork.GetUsersRepository);
    private readonly Lazy<IForumRepository> forumRepository = new(unitOfWork.GetForumRepository);
    private readonly Lazy<IClaimsRepository> claimsRepository = new(unitOfWork.GetClaimsRepository);

    private IUserRepository UserRepository => userRepository.Value;
    private IForumRepository ForumRepository => forumRepository.Value;
    private IClaimsRepository ClaimsRepository => claimsRepository.Value;

    // Именно ChangeClaimAsync: у ChangeClaim есть перегрузка Func<ctx, TResult>, и async-лямбда
    // связалась бы с ней (TResult = Task), то есть внутренний Task никто бы не ждал — исключения
    // операции терялись бы, а сохранение не происходило.
    public Task CheckInClaim(ClaimIdentification claimId, int money)
        => characterPropsService.ChangeClaimAsync(
            claimId,
            // TODO(#4891) нужно специфическое право. Перенесено как есть: до миграции это был
            // LoadClaimAsMaster(claimId) без дополнительных требований.
            ClaimAccessRequirement.AnyMaster,
            ProjectActiveRequirement.MustBeActive,
            money,
            async ctx =>
            {
                // Проверяем заранее, а пишем статус после приёма денег — как и до миграции.
                ctx.Claim.EnsureCanChangeStatus(ClaimStatus.CheckedIn);

                // Правила регистрации считаются по доменным сущностям (#4892): персонаж и заявка
                // уже лежат в контексте, профиль игрока нужен контексту проблем целиком.
                var validator = new ClaimCheckInValidator(
                    new ClaimProblemContext(
                        ctx.CharacterInfo,
                        ctx.ClaimInfo,
                        await UserRepository.GetRequiredUserInfo(ctx.ClaimInfo.PlayerId)),
                    claimValidator);
                if (!validator.CanCheckInInPrinciple)
                {
                    throw new ClaimWrongStatusException(ctx.Claim.GetId(), ctx.Claim.ClaimStatus);
                }

                if (ctx.Request > 0)
                {
                    var paymentType = ctx.ProjectInfo.ProjectFinanceSettings
                        .GetCashPaymentType(ctx.CurrentUser.UserIdentification)
                        ?? throw new JoinRpgInvalidUserException();

                    // Деньги принимаются ДО смены статуса: приём гасит взнос, и только после него
                    // validator.CanCheckInNow может стать истинным.
                    ctx.AcceptFee(".", ctx.Now, ctx.Request, paymentType);
                }
                else if (ctx.Request < 0)
                {
                    throw new InvalidOperationException();
                }

                if (!validator.CanCheckInNow)
                {
                    throw new ClaimWrongStatusException(ctx.Claim.GetId(), ctx.Claim.ClaimStatus);
                }

                ctx.ChangeStatus(ctx.Claim, ClaimStatus.CheckedIn);
                ctx.MarkChanged(ctx.Character);
                ctx.Character.InGame = true;

                _ = ctx.AddComment("", CommentExtraAction.CheckedIn, ClaimOperationType.MasterVisibleChange);
            });

    /// <summary>
    /// Выход на вторую роль: зарегистрированная заявка выходит из игры, а на другого персонажа
    /// создаётся новая — сразу утверждённая и без взноса.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Идёт через общий путь создания заявки (ADR014), поэтому сохранений два, а не одно: комментарий
    /// к новой заявке создаётся между ними. Обе мутации — и новая заявка, и старая — уезжают в первое
    /// сохранение, то есть операция не стала менее целостной, чем была.
    /// </para>
    /// <para>
    /// Валидация переноса (<c>EnsureCanMoveClaim</c>) не включается: до миграции она была
    /// закомментирована, и <see cref="ClaimOperation.MoveToSecondRole"/> заведён ровно затем, чтобы
    /// общий путь не включил её молча.
    /// </para>
    /// <para>
    /// Уведомлений уходит два: по старой заявке (как и до миграции) и по новой. Второе — изменение
    /// поведения: до миграции комментарий второй роли создавался молча. Решено по ревью #4840 —
    /// заглушать его не за что.
    /// </para>
    /// </remarks>
    public async Task<int> MoveToSecondRole(ClaimIdentification claimId, CharacterIdentification characterId, string secondRoleCommentText)
    {
        if (claimId.ProjectId != characterId.ProjectId)
        {
            throw new InvalidOperationException("Нельзя смешивать разные проекты в запросе");
        }

        // Игрока читаем заранее, до входа в мутацию: иначе CreateClaim пришлось бы уметь случай
        // «игрок будет известен позже», а он нужен ровно этой одной операции.
        var playerId = new UserIdentification(
            (await ClaimsRepository.GetClaim(claimId)
                ?? throw new JoinRpgEntityNotFoundException(claimId.ClaimId, nameof(Claim)))
            .PlayerUserId);

        var claim = await characterPropsService.CreateClaimAsync(
            characterId,
            playerId,
            ClaimOperation.MoveToSecondRole,
            //TODO Specific right. Право перенесено как есть: до миграции это был
            // LoadClaimAsMaster(claimId) без дополнительных требований.
            ClaimAccessRequirement.AnyMaster,
            ProjectActiveRequirement.MustBeActive,
            (ClaimId: claimId, CommentText: secondRoleCommentText),
            async ctx =>
            {
                var oldClaim = await ctx.LoadOtherClaim(ctx.Request.ClaimId);
                oldClaim.EnsureStatus(ClaimStatus.CheckedIn);

                // Игрок уходит со старой роли.
                oldClaim.Character.InGame = false;
                ctx.MarkChanged(oldClaim.Character);

                // Проверка перехода добавлена миграцией и заведомо проходит: выше стоит
                // EnsureStatus(CheckedIn), а CheckedIn → Approved таблицей разрешён. Отметку даты
                // писать нельзя — перезаписался бы MasterAcceptedDate, видимый в отчётах.
                ctx.ChangeStatusKeepingTimestamps(oldClaim, ClaimStatus.Approved);

                ctx.MarkChanged(ctx.Character);

                var newClaim = ctx.NewClaim(ClaimStatus.Approved, ctx.ResponsibleMasterByProjectRules());

                // Навигацию Claim.Player читает FieldSaveHelper у утверждённой заявки (имя персонажа
                // по имени игрока), а у только что созданной её ещё нет. Берём сущность из старой
                // заявки, где она уже загружена, — на relationship fixup EF полагаться нельзя.
                newClaim.Player = oldClaim.Player;

                // Разрешение на чувствительные данные переносится со старой заявки. Его даёт сам
                // игрок, а мастер за него не может: если не перенести, разрешение молча потеряется и
                // проект потребует паспорт заново — у того же игрока, который его уже дал.
                newClaim.PlayerAllowedSenstiveData = oldClaim.PlayerAllowedSenstiveData;

                // Вторую роль выдаёт мастер, значит заявка утверждена прямо сейчас и взноса не несёт.
                newClaim.MasterAcceptedDate = ctx.Now;
                newClaim.CurrentFee = 0;

                ctx.Character.ApprovedClaim = newClaim;

                // Комментарии — в порядке операций: сначала игрок вышел со старой роли, потом
                // получил новую.
                _ = ctx.AddComment(
                        oldClaim,
                        ctx.Request.CommentText,
                        CommentExtraAction.OutOfGame,
                        ClaimOperationType.MasterVisibleChange)
                    .Decorate(notification => notification with { AnotherCharacterId = characterId });

                _ = ctx.AddComment(
                        ctx.Request.CommentText,
                        CommentExtraAction.SecondRole,
                        ClaimOperationType.MasterVisibleChange);

                // Пересохранение пустым слоем — ради побочных эффектов: значений по умолчанию и
                // пересчёта спецгрупп.
                _ = ctx.SaveFields(newClaim, FieldLayerContainer.Empty(ctx.ProjectInfo));

                return newClaim;
            });

        return claim.ClaimId;
    }

    public async Task<ClaimIdentification> AddClaimFromUser(CharacterIdentification characterId,
        string claimText,
        FieldLayerContainer fields, bool sensitiveDataAllowed)
    {
        ArgumentNullException.ThrowIfNull(characterId);
        ArgumentNullException.ThrowIfNull(claimText);

        logger.LogDebug("About to add claim to character {characterId}", characterId);

        // Существование упомянутых пользователей проверяется до сохранения: FieldSaveHelper
        // синхронный и репозиториев не видит (ADR017 §7).
        await userFieldValidator.ValidateUserFields(fields);
        await fieldSetupService.MarkFieldsUsedIfNotUsedYet(fields);

        var claim = await characterPropsService.CreateClaim(
            characterId,
            currentUserAccessor.UserIdentification,
            ClaimOperation.AddByPlayer,
            // Проверки мастерских прав нет — её заменяют правила ClaimValidator.
            ClaimAccessRequirement.NoCheck,
            ProjectActiveRequirement.MustBeActive,
            (claimText, fields, sensitiveDataAllowed),
            ctx =>
            {
                var claim = ctx.NewClaim(ClaimStatus.AddedByUser, ctx.ResponsibleMasterByProjectRules());

                // Разрешение даёт игрок, и только если проект его вообще спрашивает.
                claim.PlayerAllowedSenstiveData = ctx.Request.sensitiveDataAllowed
                    && ctx.ProjectInfo.ProfileRequirementSettings.SensitiveDataRequired;

                _ = ctx.SaveFields(claim, ctx.Request.fields);

                //TODO добавить сюда измененные поля
                _ = ctx.AddComment(
                    ctx.Request.claimText,
                    CommentExtraAction.NewClaim,
                    ClaimOperationType.PlayerChange);

                return claim;
            });

        var claimId = claim.GetId();

        // Автоприём — отдельная операция, идущая строго ПОСЛЕ создания: реентерабельность
        // запрещена (ADR014, §7). Условия он перечитывает сам.
        await AutoApproveIfRequired(claimId);

        logger.LogInformation("Claim ({claimId}) was successfully send to character {characterId}", claimId, characterId);
        return claimId;
    }

    /// <summary>
    /// Обычный комментарий к заявке — без побочных действий. Модерация финансовой операции —
    /// отдельная операция, <see cref="ModerateFinanceOperation"/>.
    /// </summary>
    /// <remarks>
    /// Единственная claim-операция с <see cref="ProjectActiveRequirement.AllowInactive"/>: по ADR014
    /// комментирование в архивном проекте остаётся разрешённым — обсуждение игры продолжается после
    /// её конца, и форма комментария в UI намеренно не спрятана.
    /// </remarks>
    public Task AddComment(ClaimIdentification claimId, int? parentCommentId, bool isVisibleToPlayer, string commentText)
        => characterPropsService.ChangeClaim(
            claimId,
            ClaimAccessRequirement.MasterOrPlayer,
            ProjectActiveRequirement.AllowInactive,
            (ParentCommentId: parentCommentId,
                IsVisibleToPlayer: isVisibleToPlayer,
                CommentText: commentText),
            ctx =>
            {
                // Комментарий может писать и игрок, и мастер; видимость выбирает автор — от этих
                // двух вещей зависит и тип операции, и перевод заявки в обсуждение.
                var claimOperationType = ctx.Claim.PlayerUserId == ctx.CurrentUser.UserId
                    ? ClaimOperationType.PlayerChange
                    : ctx.Request.IsVisibleToPlayer
                        ? ClaimOperationType.MasterVisibleChange
                        : ClaimOperationType.MasterSecretChange;

                ctx.MarkDiscussed(ctx.Request.IsVisibleToPlayer);

                var parentComment = ctx.Request.ParentCommentId is int parentCommentId
                    ? FindParentComment(ctx, parentCommentId)
                    : null;

                AddCommentCore(ctx, parentComment, ctx.Request.CommentText, extraAction: null, claimOperationType);
            });

    /// <summary>
    /// Модерация финансовой операции, предложенной в комментарии: одобрение или отклонение.
    /// Сопровождается комментарием мастера с проставленным <see cref="CommentExtraAction"/>.
    /// </summary>
    /// <remarks>
    /// Три вещи, которыми модерация отличается от обычного комментария и которые поэтому не
    /// параметры, а константы операции:
    /// <list type="number">
    /// <item>её всегда делает мастер — отсюда <see cref="ClaimOperationType.MasterVisibleChange"/>
    /// без ветки «игрок»;</item>
    /// <item>её результат всегда виден игроку — он платил, он и должен узнать решение;</item>
    /// <item>она не переводит заявку в «обсуждается»: <c>MarkDiscussed</c> — про ответ
    /// противоположной стороны по существу заявки, а не про деньги.</item>
    /// </list>
    /// Требование активности проекта то же, что у <see cref="AddComment"/>: модерация приходит
    /// комментарием, а комментировать архивный проект разрешено (ADR014). Точный доступ — ниже по
    /// телу: <see cref="Permission.CanManageMoney"/> либо владелец способа оплаты, и то, и другое
    /// возможно только у мастера, отсюда <see cref="ClaimAccessRequirement.AnyMaster"/> снаружи.
    /// </remarks>
    public Task ModerateFinanceOperation(ClaimIdentification claimId, int parentCommentId, string commentText, FinanceOperationAction financeAction)
        => characterPropsService.ChangeClaim(
            claimId,
            ClaimAccessRequirement.AnyMaster,
            ProjectActiveRequirement.AllowInactive,
            (ParentCommentId: parentCommentId,
                CommentText: commentText,
                FinanceAction: financeAction),
            ctx =>
            {
                var parentComment = FindParentComment(ctx, ctx.Request.ParentCommentId)
                    ?? throw new InvalidOperationException("Requested to perform finance operation on parent comment, but there is no any");

                var finance = parentComment.Finance
                    ?? throw new InvalidOperationException("Requested to moderate comment without finance operation");

                if (!finance.RequireModeration)
                {
                    throw new ValueAlreadySetException("Finance entry is already moderated.");
                }

                // Модерирует либо мастер с правом на деньги, либо мастер — владелец способа
                // оплаты: поступления на свой счёт он подтверждает сам.
                if (!ctx.ProjectInfo.HasMasterAccess(ctx.CurrentUser, Permission.CanManageMoney)
                    && finance.PaymentType?.UserId != ctx.CurrentUser.UserId)
                {
                    throw new NoAccessToProjectException(ctx.ProjectInfo, ctx.CurrentUser.UserId);
                }

                finance.Changed = ctx.Now;

                CommentExtraAction extraAction;
                switch (ctx.Request.FinanceAction)
                {
                    case FinanceOperationAction.Approve:
                        finance.State = FinanceOperationState.Approved;
                        if (finance.OperationType == FinanceOperationType.PreferentialFeeRequest)
                        {
                            ctx.Claim.PreferentialFeeUser = true;
                        }
                        ctx.Claim.UpdateClaimFeeIfRequired(finance.OperationDate, ctx.ProjectInfo);
                        extraAction = CommentExtraAction.ApproveFinance;
                        break;
                    case FinanceOperationAction.Decline:
                        finance.State = FinanceOperationState.Declined;
                        extraAction = CommentExtraAction.RejectFinance;
                        break;
                    case FinanceOperationAction.None:
                    default:
                        throw new ArgumentOutOfRangeException(nameof(financeAction), financeAction, null);
                }

                AddCommentCore(ctx, parentComment, ctx.Request.CommentText, extraAction, ClaimOperationType.MasterVisibleChange);
            });

    /// <summary>
    /// Родительский комментарий этой же дискуссии, если он есть.
    /// </summary>
    /// <remarks>
    /// Комментарии дискуссии грузит write-хэндл (<c>Include(c => c.CommentDiscussion.Comments)</c>),
    /// иначе родителя было бы не найти и финансовая модерация тихо ломалась бы.
    /// </remarks>
    private static Comment? FindParentComment(ClaimMutationContext ctx, int parentCommentId)
        => ctx.Claim.CommentDiscussion.Comments.SingleOrDefault(c => c.CommentId == parentCommentId);

    /// <summary>
    /// Общая часть обеих операций комментирования: сам комментарий и привязка к родителю.
    /// </summary>
    private static void AddCommentCore(
        ClaimMutationContext ctx,
        Comment? parentComment,
        string commentText,
        CommentExtraAction? extraAction,
        ClaimOperationType claimOperationType)
    {
        var pending = ctx.AddComment(commentText, extraAction, claimOperationType);

        if (parentComment is not null)
        {
            // Внутри — проверка «нельзя ответить на скрытый комментарий так, чтобы игрок
            // ответ увидел».
            _ = pending.SetParent(parentComment, claimOperationType);
        }
    }

    /// <summary>
    /// Утверждение заявки мастером. Тело вынесено в <see cref="ApproveCore"/>: его же использует
    /// автоприём (<see cref="AutoApproveIfRequired"/>), который открывает собственную мутацию —
    /// вкладывать одну мутацию в другую запрещено (ADR014, §7).
    /// </summary>
    public Task ApproveByMaster(ClaimIdentification claimId, string commentText)
        => characterPropsService.ChangeClaimAsync(
            claimId,
            ClaimAccessRequirement.ApprovalDecline,
            ProjectActiveRequirement.MustBeActive,
            commentText,
            ApproveCore);

    /// <summary>Само утверждение. Аргумент операции — текст комментария мастера.</summary>
    private static async Task ApproveCore(ClaimMutationContext<string> ctx)
    {
        // Отдельная проверка, а не общая ctx.ChangeStatus: таблица переходов разрешает
        // CheckedIn -> Approved, и это не ошибка — так возвращается заявка при выходе
        // игрока на вторую роль (MoveToSecondRole). Утверждать же заявку, по которой игрок
        // уже зарегистрирован на игре, нельзя, поэтому здесь правило строже общего.
        if (ctx.Claim.ClaimStatus == ClaimStatus.CheckedIn)
        {
            throw new ClaimWrongStatusException(ctx.Claim.GetId(), ctx.Claim.ClaimStatus);
        }

        if (ctx.Claim.Character.CharacterType == CharacterType.Slot)
        {
            // Единственная асинхронная загрузка метода, и она условная: сюжеты нужны только
            // слоту. Поднимать её наверх нельзя — грузились бы на каждое утверждение.
            var character = await CreateCharacterFromSlot(ctx, ctx.Claim.Character, ctx.Claim.Player);
            ctx.Claim.Character = character;
            ctx.Claim.CharacterId = character.CharacterId;
        }

        ctx.ChangeStatus(ctx.Claim, ClaimStatus.Approved);

        _ = ctx.AddComment(
            ctx.Request,
            CommentExtraAction.ApproveByMaster,
            ClaimOperationType.MasterVisibleChange);

        if (ctx.ProjectInfo.ClaimSettings.StrictlyOneCharacter)
        {
            // Читает claim.Player.Claims — навигацию, которую write-хэндл грузит явно
            // (Include(c => c.Player.Claims)). Без неё список молча оказался бы пуст.
            foreach (var otherClaim in ctx.Claim.OtherPendingClaimsForThisPlayer())
            {
                ctx.ChangeStatus(otherClaim, ClaimStatus.DeclinedByMaster);

                _ = ctx.AddComment(
                    otherClaim,
                    //TODO[Localize]
                    "Заявка автоматически отклонена, т.к. другая заявка того же игрока была принята в тот же проект",
                    CommentExtraAction.DeclineByMaster,
                    ClaimOperationType.MasterVisibleChange);
            }
        }

        ctx.MarkCharacterChangedIfApproved();
        ctx.Claim.Character.ApprovedClaimId = ctx.Claim.ClaimId;
        // Порядок критичен: ApprovedClaim обязан быть проставлен ДО SaveFields — от него
        // зависит выбор стратегии в FieldSaveHelper (IsApproved => SaveToCharacterAndClaim).
        ctx.Claim.Character.ApprovedClaim = ctx.Claim;
        ctx.Claim.Character.IsHot = false;

        // Пересохранение пустым слоем — не мёртвый код, оно нужно ради побочных эффектов:
        // 1. если персонаж создан из слота при утверждении, ему надо проставить имя;
        // 2. часть значений полей переезжает из заявки в персонажа;
        // 3. (2) может пересчитать спецгруппы.
        // Показывать изменённые поля в письме не надо, поэтому результат игнорируем.
        _ = ctx.SaveFields(FieldLayerContainer.Empty(ctx.ProjectInfo));
    }

    /// <summary>
    /// Автоприём заявки в проекте, где включён <c>AutoAcceptClaims</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Вызывается строго ПОСЛЕ завершения операции, создавшей или принявшей заявку: вкладывать
    /// одну мутацию в другую запрещено (ADR014, §7). Порядок побочных эффектов при этом сохранён —
    /// и до миграции автоприём шёл после рассылки уведомлений.
    /// </para>
    /// <para>
    /// Условия перечитываются из базы, а не берутся из мутированного EF-графа предыдущей операции.
    /// Читать их приходится до входа в мутацию: утверждает ответственный мастер, узнать его можно
    /// только из заявки, а подмена пользователя обязана случиться раньше проверки прав.
    /// </para>
    /// </remarks>
    private async Task AutoApproveIfRequired(ClaimIdentification claimId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(claimId.ProjectId);

        if (!projectInfo.ClaimSettings.AutoAcceptClaims)
        {
            return;
        }

        var claim = await ClaimsRepository.GetClaim(claimId)
            ?? throw new JoinRpgEntityNotFoundException(claimId.ClaimId, nameof(Claim));

        // Не принимаем автоматически заявки, если игрок не предоставил доступ к паспорту
        if (!claim.PlayerAllowedSenstiveData && projectInfo.ProfileRequirementSettings.SensitiveDataRequired)
        {
            logger.LogInformation(
                "Claim ({claimId}) was not auto-approved: sensitive data access is required but not granted by player",
                claimId);
            return;
        }

        // Автоприём применим не к любой заявке. Подача и принятие приглашения всегда приводят
        // сюда с заявкой «в обсуждении», а вот разрешение доступа к паспорту игрок может дать
        // по заявке в любом статусе — уже принятой, отклонённой, зарегистрированной. Принимать
        // там нечего, и без этой проверки ApproveCore просто бросил бы ClaimWrongStatusException.
        if (claim.ClaimStatus is not (ClaimStatus.AddedByUser or ClaimStatus.Discussed))
        {
            logger.LogInformation(
                "Claim ({claimId}) was not auto-approved: claim status is {claimStatus}",
                claimId,
                claim.ClaimStatus);
            return;
        }

        var responsibleMaster = await UserRepository.GetRequiredUserInfo(
            new UserIdentification(claim.ResponsibleMasterUserId));

        impersonateAccessor.StartImpersonate(responsibleMaster.UserId, responsibleMaster.DisplayName, responsibleMaster.IsAdmin);
        try
        {
            //TODO[Localize]
            await ApproveByMaster(claimId, "Ваша заявка была принята автоматически");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Claim ({claimId}) auto-approve failed", claimId);
            throw;
        }
        finally
        {
            impersonateAccessor.StopImpersonate();
        }
    }

    /// <summary>
    /// Создаёт персонажа из слота при утверждении заявки: уменьшает остаток слота, копирует
    /// свойства и наследует прямые привязки к сюжетам.
    /// </summary>
    /// <remarks>
    /// <c>CreatedAt</c>/<c>UpdatedAt</c> проставляются локальным <see cref="DateTime.Now"/>, а не
    /// временем операции (UTC). Это выглядит ошибкой, но сохранено как есть: миграция обязана
    /// сохранять поведение, а починка — отдельное изменение.
    /// </remarks>
    private static async Task<Character> CreateCharacterFromSlot(ClaimMutationContext ctx, Character slot, User player)
    {
        switch (slot.CharacterSlotLimit)
        {
            case null:  // Unlimited slot
                break;
            case > 0:
                slot.CharacterSlotLimit--;
                break;
            default:
                throw new JoinRpgSlotLimitedException(slot);
        }

        if (slot.CharacterType != CharacterType.Slot)
        {
            throw new EntityWrongStatusException(slot);
        }

        var newCharacter = new Character()
        {
            ApprovedClaim = null,
            ApprovedClaimId = null,
            AutoCreated = true,
            OriginalCharacterSlot = slot,
            CanBePermanentlyDeleted = false,
            CharacterId = -1,
            CharacterName = slot.CharacterName, // Probably will be updated by field save
            CharacterSlotLimit = null,
            CharacterType = CharacterType.Player,
            CreatedAt = DateTime.Now,
            CreatedBy = player,
            CreatedById = player.UserId,
            DirectlyRelatedPlotElements = slot.DirectlyRelatedPlotElements,
            HidePlayerForCharacter = slot.HidePlayerForCharacter,
            InGame = false,
            IsAcceptingClaims = true,
            IsActive = true,
            IsHot = false,
            IsPublic = slot.IsPublic,
            JsonData = slot.JsonData,
            ParentCharacterGroupIds = slot.ParentCharacterGroupIds,
            PlotElementOrderData = slot.PlotElementOrderData,
            Project = slot.Project,
            ProjectId = slot.ProjectId,
            Subscriptions = slot.Subscriptions,
            UpdatedAt = DateTime.Now,
            UpdatedBy = player,
            UpdatedById = player.UserId,
        };

        // Через контекст, а не через UnitOfWork.GetDbSet: сохраняет тот DbContext, который загрузил
        // агрегат, а DI-экземпляр UnitOfWork у сервиса — другой (ADR014).
        ctx.AddEntity(newCharacter);

        var plots = await ctx.LoadDirectPlotsForCharacter(slot.GetId());

        foreach (var plot in plots)
        {
            if (plot.TargetCharacters.Contains(slot))
            {
                plot.TargetCharacters.Add(newCharacter);
            }
        }

        return newCharacter;
    }

    public Task DeclineByMaster(ClaimIdentification claimId, ClaimDenialReason claimDenialStatus, string commentText, bool deleteCharacter)
        => characterPropsService.ChangeClaimAsync(
            claimId,
            ClaimAccessRequirement.ApprovalDecline,
            ProjectActiveRequirement.MustBeActive,
            (DenialStatus: claimDenialStatus, CommentText: commentText, DeleteCharacter: deleteCharacter),
            async ctx =>
            {
                // Запоминаем ДО смены статуса: удалять персонажа можно только у утверждённой заявки.
                var statusWasApproved = ctx.Claim.ClaimStatus == ClaimStatus.Approved;

                ctx.ChangeStatus(ctx.Claim, ClaimStatus.DeclinedByMaster);
                ctx.Claim.ClaimDenialStatus = ctx.Request.DenialStatus;

                // Сбрасываем это при отклонении заявки, если заявку восстановить, надо будет повторно получать разрешение
                ctx.Claim.PlayerAllowedSenstiveData = false;

                var roomNotification = CommonClaimDecline(ctx);

                if (ctx.Request.DeleteCharacter)
                {
                    if (!statusWasApproved)
                    {
                        throw new InvalidOperationException("Attempt to delete character, but it not exists");
                    }
                    DeleteCharacter(ctx);
                }

                await DeclineAllClaimInvites(ctx);

                _ = ctx.AddComment(
                    ctx.Request.CommentText,
                    CommentExtraAction.DeclineByMaster,
                    ClaimOperationType.MasterVisibleChange);

                if (roomNotification is not null)
                {
                    // Порядок «сначала уведомления по комментариям, потом о проживании» обеспечивает сервис.
                    ctx.AddRoomNotification(roomNotification);
                }
            });

    /// <summary>
    /// Деактивация персонажа вместе с отклонением заявки.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Вторая проверка прав внутри лямбды — намеренный escape-hatch (ADR014, §5): право зависит от
    /// аргумента операции (<c>deleteCharacter</c>), а требование доступа самой операции
    /// (<see cref="ClaimAccessRequirement.ApprovalDecline"/>) его не покрывает.
    /// </para>
    /// <para>
    /// Связи с сюжетами здесь <b>не рвутся</b>. Раньше рвались — и только здесь: при обычном
    /// удалении персонажа очистка стояла под <c>Character.CanBePermanentlyDeleted</c>, а это
    /// public-поле со значением <c>false</c>, которое ничему другому не присваивается и в БД не
    /// отображается, так что та ветка была мертва. Расхождение «никогда против всегда» убрано в
    /// пользу сохранения данных: удаление персонажа здесь мягкое (<c>IsActive = false</c>), заявку
    /// умеет вернуть <c>RestoreByMaster</c>, а оборванные связи с сюжетами не вернулись бы.
    /// </para>
    /// </remarks>
    private static void DeleteCharacter(ClaimMutationContext ctx)
    {
        _ = ctx.ProjectInfo.RequestMasterAccess(ctx.CurrentUser, Permission.CanEditRoles);

        ctx.Character.IsActive = false;
        ctx.MarkChanged(ctx.Character);
    }

    /// <summary>
    /// Общая часть отклонения заявки на контексте мутации (ADR014). Возвращает уведомление о выезде
    /// из комнаты, если заявка была поселена.
    /// </summary>
    private static RoomOccupancyNotification? CommonClaimDecline(ClaimMutationContext ctx)
    {
        ctx.MarkCharacterChangedIfApproved();

        if (ctx.Claim.Character?.ApprovedClaim == ctx.Claim)
        {
            ctx.Claim.Character.ApprovedClaimId = null;
        }

        return ConsiderLeavingRoom(ctx, RoomOccupancyChangeKind.ClaimDeclined);
    }

    /// <summary>
    /// Выселяет заявку из комнаты при отклонении: инициатор берётся
    /// из уже загруженного хэндла (лишнего запроса за текущим пользователем нет), а опустевшая заявка
    /// на поселение удаляется через тот же <c>DbContext</c>, а не через сырой <c>DbSet</c>.
    /// </summary>
    private static RoomOccupancyNotification? ConsiderLeavingRoom(
        ClaimMutationContext ctx,
        RoomOccupancyChangeKind kind)
    {
        var claim = ctx.Claim;

        if (claim.AccommodationRequest is null)
        {
            return null;
        }

        RoomOccupancyNotification? notification = null;

        if (claim.AccommodationRequest.Accommodation is { } room)
        {
            var claimId = ctx.ClaimInfo.ClaimId;

            notification = new RoomOccupancyNotification(
                room.GetId(),
                room.Name,
                // Название категории сервис уведомлений возьмёт из метаданных: по навигации
                // room.ProjectAccommodationType это была бы лишняя ленивая загрузка. Колонка комнаты
                // хранит её категорию (ADR018, §2, пункт 1), а не тип проживания группы.
                new RoomCategoryIdentification(claimId.ProjectId, room.AccommodationTypeId),
                // Из текущего запроса, а не из ctx.Initiator: EF-сущность пользователя нужна была
                // только легаси-письмам.
                ctx.CurrentUser.ToUserInfoHeader(),
                Changed: [claimId],
                // Соседи — состав комнаты без выезжающего. Считается до изъятия заявки из группы,
                // поэтому её и исключаем явно.
                Remaining: [.. room.Inhabitants
                    .SelectMany(group => group.GetSubjectIds())
                    .Where(id => id != claimId)],
                kind);
        }

        _ = claim.AccommodationRequest.Subjects.Remove(claim);
        if (claim.AccommodationRequest.Subjects.Count == 0)
        {
            ctx.RemoveEntity(claim.AccommodationRequest);
        }

        return notification;
    }

    /// <summary>
    /// Отклоняет все приглашения к совместному проживанию, в которых участвует заявка, и ставит
    /// уведомление остальным участникам в очередь.
    /// </summary>
    /// <remarks>
    /// Раньше это делал <c>AccommodationInviteServiceImpl.DeclineAllClaimInvites</c> на собственном
    /// <c>DbContext</c> и собственным <c>SaveChanges</c> — приглашения коммитились независимо от
    /// внешней операции. Теперь и запрос, и мутация идут через контекст, значит уезжают в то же
    /// единственное сохранение (ADR014).
    /// </remarks>
    private static async Task DeclineAllClaimInvites(ClaimMutationContext ctx)
    {
        var invites = await ctx.LoadInvitesForClaim();
        if (invites.Count == 0)
        {
            return;
        }

        var recipients = new List<Claim>();
        foreach (var invite in invites)
        {
            invite.IsAccepted = InviteState.Declined;
            invite.ResolveDescription = ResolveDescription.ClaimCanceled;

            foreach (var participant in new[] { invite.From, invite.To })
            {
                // Сама отклоняемая заявка уведомления о себе не получает.
                if (participant is not null
                    && participant.ClaimId != ctx.Claim.ClaimId
                    && !recipients.Contains(participant))
                {
                    recipients.Add(participant);
                }
            }
        }

        if (recipients.Count == 0)
        {
            return;
        }

        var notification = new AccommodationInviteNotification(
            [.. recipients.Select(claim => claim.GetId())],
            ctx.CurrentUser.ToUserInfoHeader(),
            InviteChangeKind.Cancelled);

        ctx.AddInviteNotification(notification);
    }

    /// <inheritdoc />
    public Task<AccommodationRequest?> LeaveAccommodationGroupAsync(int projectId, int claimId)
        => characterPropsService.ChangeClaim<int, AccommodationRequest?>(
            new ClaimIdentification(projectId, claimId),
            ClaimAccessRequirement.AccommodationChange,
            ProjectActiveRequirement.MustBeActive,
            claimId,
            ctx =>
            {
                var acr = ctx.Claim.AccommodationRequest;

                // Оба ранних выхода обязаны остаться холостыми: до миграции они возвращали ответ,
                // ничего не сохраняя.
                if (acr is null)
                {
                    ctx.NothingChanged();
                    return null;
                }

                if (acr.Subjects.Count == 1)
                {
                    ctx.NothingChanged();
                    return acr;
                }

                // TODO: восстановить отправку изменений полей, см. ADR014

                var leaveNotification = ConsiderLeavingRoom(ctx, RoomOccupancyChangeKind.LeftRoom);

                ctx.Claim.AccommodationRequest_Id = null;
                ctx.Claim.AccommodationRequest = null;

                ctx.AddEntity(new AccommodationRequest
                {
                    ProjectId = projectId,
                    AccommodationTypeId = acr.AccommodationTypeId,
                    IsAccepted = InviteState.Accepted,
                    Subjects = [ctx.Claim],
                });

                if (leaveNotification is not null)
                {
                    ctx.AddRoomNotification(leaveNotification);
                }

                return acr;
            });

    public Task<AccommodationRequest> SetAccommodationType(int projectId,
        int claimId,
        int roomTypeId)
        //todo set first state to Unanswered
        => characterPropsService.ChangeClaimAsync<int, AccommodationRequest>(
            new ClaimIdentification(projectId, claimId),
            ClaimAccessRequirement.AccommodationChange,
            ProjectActiveRequirement.MustBeActive,
            roomTypeId,
            async ctx =>
            {
                // TODO: игрок не должен менять тип поселения после регистрации. Проверки нет и
                // никогда не было: комментарий-намерение висит здесь с 2018 года (70ccb4a11), кода
                // под ним не появилось. Оставлено как есть — это изменение поведения, не рефакторинг.

                if (ctx.Claim.AccommodationRequest?.AccommodationTypeId == roomTypeId)
                {
                    // Тип уже такой — операции нет, как и до миграции.
                    ctx.NothingChanged();
                    return ctx.Claim.AccommodationRequest;
                }

                // Проверка, что тип поселения существует в этом проекте. Метаданные уже на руках
                // (ADR015), отдельный запрос за сущностью не нужен: мутируем мы AccommodationRequest,
                // а сам тип только называем по идентификатору.
                _ = ctx.ProjectInfo.AccommodationSettings.GetTypeById(
                    new AccommodationTypeIdentification(ctx.ProjectInfo.ProjectId, roomTypeId));

                // TODO: восстановить отправку изменений полей, см. ADR014

                var leaveNotification = ConsiderLeavingRoom(ctx, RoomOccupancyChangeKind.LeftRoom);

                // TODO: Just change accommodation type if this claim is the only occupant of previous room
                var accommodationRequest = new AccommodationRequest
                {
                    ProjectId = projectId,
                    Subjects = [ctx.Claim],
                    AccommodationTypeId = roomTypeId,
                    IsAccepted = InviteState.Accepted,
                };

                ctx.AddEntity(accommodationRequest);

                if (leaveNotification is not null)
                {
                    ctx.AddRoomNotification(leaveNotification);
                }

                return accommodationRequest;
            });


    public Task DeclineByPlayer(ClaimIdentification claimId, string commentText)
        => characterPropsService.ChangeClaimAsync(
            claimId,
            ClaimAccessRequirement.PlayerOnly,
            ProjectActiveRequirement.MustBeActive,
            commentText,
            async ctx =>
            {
                ctx.ChangeStatus(ctx.Claim, ClaimStatus.DeclinedByUser);

                // Сбрасываем это при отклонении заявки, если заявку восстановить, надо будет повторно получать разрешение
                ctx.Claim.PlayerAllowedSenstiveData = false;

                await DeclineAllClaimInvites(ctx);

                var roomNotification = CommonClaimDecline(ctx);

                _ = ctx.AddComment(
                    ctx.Request,
                    CommentExtraAction.DeclineByPlayer,
                    ClaimOperationType.PlayerChange);

                if (roomNotification is not null)
                {
                    // Порядок «сначала уведомления по комментариям, потом о проживании» обеспечивает сервис.
                    ctx.AddRoomNotification(roomNotification);
                }
            });

    public Task RestoreByMaster(ClaimIdentification claimId, string commentText, CharacterIdentification characterId)
        => characterPropsService.ChangeClaimAsync(
            claimId,
            ClaimAccessRequirement.ApprovalDecline,
            ProjectActiveRequirement.MustBeActive,
            (CommentText: commentText, CharacterId: characterId),
            async ctx =>
            {
                var oldCharacterId = ctx.Claim.GetCharacterId(); // Сохраняем на случай если он изменится
                var (character, _) = await ctx.LoadOtherCharacter(ctx.Request.CharacterId);

                ctx.ChangeStatus(ctx.Claim, ClaimStatus.AddedByMaster);
                ctx.Claim.ClaimDenialStatus = null;
                // Мастер не может дать разрешение на чувствительные данные от имени игрока
                ctx.Claim.PlayerAllowedSenstiveData = false;
                ctx.MarkDiscussed(isVisibleToPlayer: true);

                if (character.ApprovedClaim is not null)
                {
                    // Персонаж, куда мы пытаемся восстановить заявку, уже занят.
                    throw new ClaimTargetIsNotAcceptingClaims();
                }

                ctx.Claim.Character = character;
                ctx.Claim.CharacterId = ctx.Request.CharacterId.Id;

                //Ensure that character is active
                character.IsActive = true;
                ctx.MarkChanged(character);

                _ = ctx.AddComment(
                        ctx.Request.CommentText,
                        CommentExtraAction.RestoreByMaster,
                        ClaimOperationType.MasterVisibleChange)
                    .Decorate(notification => notification with { AnotherCharacterId = oldCharacterId });
            });

    public Task MoveByMaster(ClaimIdentification claimId, string commentText, CharacterIdentification characterId)
        => characterPropsService.ChangeClaimAsync(
            claimId,
            ClaimAccessRequirement.ApprovalDecline,
            ProjectActiveRequirement.MustBeActive,
            (CommentText: commentText, CharacterId: characterId),
            async ctx =>
            {
                var oldCharacterId = ctx.Claim.GetCharacterId(); // Сохраняем, так как он изменится

                // Целевой персонаж приезжает вместе со своим доменным снимком, поэтому отдельного
                // похода в ICharacterInfoRepository за правилами переноса больше нет.
                var (target, targetInfo) = await ctx.LoadOtherCharacter(ctx.Request.CharacterId);

                var userInfo = await UserRepository.GetRequiredUserInfo(ctx.ClaimInfo.PlayerId);

                // Правила считаются по доменному снимку заявки (ctx.ClaimInfo), а не по трекаемой
                // EF-сущности: снимок сделан до мутаций, так что в этой точке он с ней совпадает.
                ClaimValidator.EnsureCanMoveClaim(
                    targetInfo,
                    new UserClaimInfo(ctx.ClaimInfo.ClaimId, ctx.ClaimInfo.Status),
                    userInfo,
                    ctx.ProjectInfo);

                ctx.MarkCharacterChangedIfApproved(); // before move

                if (ctx.Claim.Character != null && ctx.Claim.IsApproved)
                {
                    ctx.Claim.Character.ApprovedClaim = null;
                }
                ctx.Claim.CharacterId = ctx.Request.CharacterId.CharacterId;
                ctx.Claim.Character = target; //That fields is required later

                if (ctx.Claim.IsApproved)
                {
                    ctx.Claim.Character.ApprovedClaim = ctx.Claim;
                }

                ctx.MarkCharacterChangedIfApproved(); // after move

                _ = ctx.AddComment(
                        ctx.Request.CommentText,
                        CommentExtraAction.MoveByMaster,
                        ClaimOperationType.MasterVisibleChange)
                    .Decorate(notification => notification with { AnotherCharacterId = oldCharacterId });
            });

    /// <summary>
    /// Отметка «прочитано до такого-то комментария».
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Намеренно не мигрирована на <c>ICharacterPropsService</c></b> (ADR014). Метод работает не с
    /// заявкой, а с дискуссией: та же отметка ставится и на форумных тредах, у которых заявки нет
    /// вовсе. Натягивать на неё агрегат персонажа означало бы требовать <c>ClaimIdentification</c>
    /// там, где его может не существовать, — и либо завести второй путь для форума, либо потерять
    /// отметку на форуме.
    /// </para>
    /// <para>
    /// По требованиям ADR014 это <c>AllowInactive</c> (отметка «прочитано» — по смыслу чтение), и
    /// именно так метод себя и ведёт: проверки активности здесь нет. Отсутствие проверки доступа —
    /// известная дыра из списка «что сознательно не чиним».
    /// </para>
    /// <para>
    /// Из базовых классов метод не берёт ничего: <c>DbContext</c> и текущий пользователь приходят
    /// из собственных зависимостей сервиса, поэтому будущее удаление <c>ClaimImplBase</c> ему не
    /// мешает.
    /// </para>
    /// </remarks>
    public async Task UpdateReadCommentWatermark(int projectId, int commentDiscussionId, int maxCommentId)
    {
        var currentUserId = currentUserAccessor.UserId;
        var watermarks =
          unitOfWork.GetDbSet<ReadCommentWatermark>()
            .Where(w => w.CommentDiscussionId == commentDiscussionId && w.UserId == currentUserId)
            .OrderByDescending(wm => wm.ReadCommentWatermarkId)
            .ToList();

        //Sometimes watermarks can duplicate. If so, let's remove them.
        foreach (var wm in watermarks.Skip(1))
        {
            _ = unitOfWork.GetDbSet<ReadCommentWatermark>().Remove(wm);
        }

        var watermark = watermarks.FirstOrDefault();

        if (watermark == null)
        {
            watermark = new ReadCommentWatermark()
            {
                CommentDiscussionId = commentDiscussionId,
                ProjectId = projectId,
                UserId = currentUserId,
            };
            _ = unitOfWork.GetDbSet<ReadCommentWatermark>().Add(watermark);
        }

        if (watermark.CommentId > maxCommentId)
        {
            return;
        }
        watermark.CommentId = maxCommentId;
        await unitOfWork.SaveChangesAsync();
    }

    public Task SetResponsible(ClaimIdentification claimId, UserIdentification responsibleMasterId)
        => characterPropsService.ChangeClaimAsync(
            claimId,
            ClaimAccessRequirement.ApprovalDecline,
            ProjectActiveRequirement.MustBeActive,
            responsibleMasterId,
            async ctx =>
            {
                // Новый ответственный обязан быть мастером проекта.
                _ = ctx.ProjectInfo.RequestMasterAccess(ctx.Request);

                var oldResponsibleMaster = ctx.ClaimInfo.ResponsibleMasterId;
                var oldMasterDisplayName = ctx.Claim.ResponsibleMasterUser.GetDisplayName();

                if (ctx.Request == oldResponsibleMaster)
                {
                    // Ровно как до миграции: молча выходим ДО мутации. Сохранения и уведомления
                    // быть не должно — иначе назначение «того же самого» начнёт шуметь.
                    ctx.NothingChanged();
                    return;
                }

                ctx.Claim.ResponsibleMasterUserId = ctx.Request;

                var newMaster = await UserRepository.GetById(ctx.Request);

                _ = ctx.AddComment(
                        $"{oldMasterDisplayName} → {newMaster.GetDisplayName()}",
                        CommentExtraAction.ChangeResponsible,
                        ClaimOperationType.MasterVisibleChange)
                    .Decorate(notification => notification with { OldResponsibleMaster = oldResponsibleMaster });
            });

    public async Task SaveFieldsFromClaim(
        ClaimIdentification claimId,
        FieldLayerContainer fieldsToSet)
    {
        await userFieldValidator.ValidateUserFields(fieldsToSet);
        await fieldSetupService.MarkFieldsUsedIfNotUsedYet(fieldsToSet);

        await characterPropsService.ChangeClaim(
            claimId,
            ClaimAccessRequirement.MasterOrPlayer,
            ProjectActiveRequirement.MustBeActive,
            fieldsToSet,
            ctx =>
            {
                var updatedFields = ctx.SaveFields(ctx.Request);

                if (updatedFields.Any(f => f.Field.BoundTo == FieldBoundTo.Character) && ctx.Claim.Character != null)
                {
                    ctx.MarkChanged(ctx.Claim.Character);
                }

                // TODO: восстановить отправку изменений полей, см. ADR014
            });
    }

    public Task OnHoldByMaster(ClaimIdentification claimId, string commentText)
        => characterPropsService.ChangeClaim(
            claimId,
            ClaimAccessRequirement.ApprovalDecline,
            ProjectActiveRequirement.MustBeActive,
            commentText,
            ctx =>
            {
                ctx.MarkCharacterChangedIfApproved();
                ctx.ChangeStatus(ctx.Claim, ClaimStatus.OnHold);

                _ = ctx.AddComment(
                    ctx.Request,
                    CommentExtraAction.OnHoldByMaster,
                    ClaimOperationType.MasterVisibleChange);
            });

    /// <summary>
    /// Сокрытие комментария от игрока.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Намеренно не мигрировано на <c>ICharacterPropsService</c></b> (ADR014) — по той же причине,
    /// что и <see cref="UpdateReadCommentWatermark"/>: операция работает с комментарием в дискуссии,
    /// а дискуссия может быть форумной. Модерация форума — не мутация агрегата персонажа, и
    /// требовать здесь <c>ClaimIdentification</c> было бы неправдой о предметной области.
    /// </para>
    /// <para>
    /// По ADR014 это <c>AllowInactive</c>: модерация должна работать всюду, где работает
    /// комментирование, а комментирование в архивном проекте разрешено. Проверки активности здесь
    /// нет — то есть требование уже выполнено.
    /// </para>
    /// <para>
    /// Права проверяются по самому комментарию (<c>HasMasterAccess</c>), а не через
    /// <c>LoadClaimAs*</c>, поэтому будущее удаление <c>ClaimImplBase</c> методу не мешает.
    /// </para>
    /// </remarks>
    public async Task ConcealComment(int projectId,
        int commentId,
        int commentDiscussionId)
    {
        var discussion = await ForumRepository.GetDiscussionByComment(projectId, commentId);
        var childComments = discussion.Comments.Where(c => c.ParentCommentId == commentId);
        var comment = discussion.Comments.FirstOrDefault(coment => coment.CommentId == commentId);

        if (comment == null)
        {
            throw new JoinRpgEntityNotFoundException(commentId, nameof(Comment));
        }

        if (comment.HasMasterAccess(currentUserAccessor) && !childComments.Any() &&
            comment.IsVisibleToPlayer && !comment.IsCommentByPlayer)
        {
            comment.IsVisibleToPlayer = false;
            await unitOfWork.SaveChangesAsync();
        }
        else
        {
            throw new JoinRpgConcealCommentException();
        }
    }

    /// <summary>
    /// Игрок разрешает доступ к чувствительным данным. Ни комментария, ни уведомления здесь нет —
    /// так было и до миграции.
    /// </summary>
    public async Task AllowSensitiveData(ClaimIdentification claimId)
    {
        await characterPropsService.ChangeClaim(
            claimId,
            ClaimAccessRequirement.PlayerOnly,
            ProjectActiveRequirement.MustBeActive,
            claimId,
            ctx =>
            {
                ctx.Claim.PlayerAllowedSenstiveData = true;
                ctx.MarkDiscussed(isVisibleToPlayer: true);
            });

        // Отсутствие доступа к паспорту — единственное, что могло удерживать автоприём
        // (см. AutoApproveIfRequired). Теперь условие выполнено, поэтому пробуем принять:
        // это отдельная операция ПОСЛЕ основной, вкладывать мутации запрещено (ADR014, §7).
        await AutoApproveIfRequired(claimId);
    }

    public async Task AcceptInvitation(ClaimIdentification claimId, string commentText, bool sensitiveDataAllowed)
    {
        await characterPropsService.ChangeClaim(
            claimId,
            ClaimAccessRequirement.PlayerOnly,
            ProjectActiveRequirement.MustBeActive,
            (CommentText: commentText, SensitiveDataAllowed: sensitiveDataAllowed),
            ctx =>
            {
                // Принять можно только приглашение от мастера. Проверка именно такая, а не через
                // EnsureStatus/таблицу переходов: она была написана руками и переносится как есть.
                if (ctx.Claim.ClaimStatus != ClaimStatus.AddedByMaster)
                {
                    throw new ClaimWrongStatusException(ctx.Claim.GetId(), ctx.Claim.ClaimStatus);
                }

                // Разрешение даёт игрок, и только если проект его вообще спрашивает.
                ctx.Claim.PlayerAllowedSenstiveData = ctx.Request.SensitiveDataAllowed
                    && ctx.ProjectInfo.ProfileRequirementSettings.SensitiveDataRequired;

                ctx.MarkDiscussed(isVisibleToPlayer: true);

                _ = ctx.AddComment(
                    ctx.Request.CommentText,
                    CommentExtraAction.InvitationAcceptedByPlayer,
                    ClaimOperationType.PlayerChange);
            });

        // Автоприём — отдельная операция, идущая строго ПОСЛЕ принятия приглашения: реентерабельность
        // запрещена (ADR014, §7). Порядок сохранён — и раньше он шёл после рассылки уведомления.
        await AutoApproveIfRequired(claimId);
    }

    /// <summary>
    /// Заявка игрока в проекте донатов: находит существующую либо подаёт новую.
    /// </summary>
    /// <remarks>
    /// Собственных мутаций у метода нет — он только ищет заявку и делегирует в уже мигрированный
    /// <see cref="AddClaimFromUser"/>, который и приносит и права, и проверку активности проекта, и
    /// единый путь создания (ADR014). Поэтому метод остаётся как есть; замещаемых
    /// <c>[Obsolete]</c>-членов он не трогает.
    /// </remarks>
    public async Task<ClaimIdentification> SystemEnsureClaim(ProjectIdentification donateProjectId)
    {
        var claims = await ClaimsRepository.GetClaimsForPlayer(donateProjectId, currentUserAccessor.UserIdentification, ClaimStatusSpec.Any);
        var claim = claims.TrySelectSingleClaim() ?? claims.FirstOrDefault();
        if (claim != null)
        {
            //TODO восстановить заявку, если она была отозвана или отклонена
            return claim.GetId();
        }
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(donateProjectId);
        if (projectInfo.ClaimSettings.DefaultTemplate is null)
        {
            logger.LogError("Некорректно настроен проект донатов {donateProjectId}", donateProjectId);
            throw new JoinRpgProjectMisconfiguredException(donateProjectId, "У проекта должен быть шаблон по умолчанию");
        }
        return await AddClaimFromUser(projectInfo.ClaimSettings.DefaultTemplate, claimText: "", fields: FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
    }

    public async Task<ClaimIdentification> AddClaimFromMaster(CharacterIdentification characterId, UserIdentification userId, string commentText, FieldLayerContainer fields)
    {
        ArgumentNullException.ThrowIfNull(characterId);
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(commentText);

        logger.LogDebug("About to add claim from master to character {characterId} for user {userId}", characterId, userId);

        await userFieldValidator.ValidateUserFields(fields);
        await fieldSetupService.MarkFieldsUsedIfNotUsedYet(fields);

        // Правила подачи проверяет сам props-сервис: ClaimOperation.AddByMaster пропускает причины
        // с MasterCanOverride — закрытый приём заявок и незаполненные контакты игрока мастера не
        // останавливают.
        var claim = await characterPropsService.CreateClaim(
            characterId,
            userId,
            ClaimOperation.AddByMaster,
            // Мастер оформляет заявку на чужое имя, поэтому нужно право управления заявками.
            ClaimAccessRequirement.ManageClaims,
            ProjectActiveRequirement.MustBeActive,
            (commentText, fields),
            ctx =>
            {
                var claim = ctx.NewClaim(ClaimStatus.AddedByMaster, ctx.ResponsibleMasterByProjectRules());

                _ = ctx.SaveFields(claim, ctx.Request.fields);

                // Комментарий о приглашении
                _ = ctx.AddComment(
                    ctx.Request.commentText,
                    CommentExtraAction.NewClaim,
                    ClaimOperationType.MasterVisibleChange);

                return claim;
            });

        var claimId = claim.GetId();
        logger.LogInformation("Claim ({claimId}) was successfully created by master for character {characterId} for user {userId}", claimId, characterId, userId);
        return claimId;
    }
}

