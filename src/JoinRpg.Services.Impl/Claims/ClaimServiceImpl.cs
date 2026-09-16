using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl.Claims;

internal class ClaimServiceImpl(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository,
    IProblemValidator<Claim> claimValidator,
    ILogger<CharacterServiceImpl> logger,
    CommentHelper commentHelper,
    ICharacterPropsService characterPropsService,
    IClaimApprovalService claimApprovalService,
    ClaimAutoApproveService claimAutoApproveService
    )
    : ClaimImplBase(unitOfWork, emailService, currentUserAccessor, projectMetadataRepository, commentHelper), IClaimService
{
    public Task CheckInClaim(ClaimIdentification claimId, int money)
        => characterPropsService.ChangeClaim(
            claimId,
            //TODO Specific right. Право перенесено как есть: до миграции это был
            // LoadClaimAsMaster(claimId) без дополнительных требований.
            ClaimAccessRequirement.AnyMaster,
            ProjectActiveRequirement.MustBeActive,
            money,
            ctx =>
            {
                // Проверяем заранее, а пишем статус после приёма денег — как и до миграции.
                ctx.EnsureCanChangeStatus(ctx.Claim, ClaimStatus.CheckedIn);

                var validator = new ClaimCheckInValidator(ctx.Claim, claimValidator, ctx.ProjectInfo);
                if (!validator.CanCheckInInPrinciple)
                {
                    throw new ClaimWrongStatusException(ctx.Claim.GetId(), ctx.Claim.ClaimStatus);
                }

                PendingComment? financeComment = null;
                if (ctx.Request > 0)
                {
                    var paymentType = ctx.ProjectInfo.ProjectFinanceSettings
                        .GetCashPaymentType(ctx.CurrentUser.UserIdentification)
                        ?? throw new JoinRpgInvalidUserException();

                    // Деньги принимаются ДО смены статуса: приём гасит взнос, и только после него
                    // validator.CanCheckInNow может стать истинным.
                    financeComment = ctx.AcceptFeeDeferringNotification(".", ctx.Now, ctx.Request, paymentType);
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

                // Порядок уведомлений — часть контракта: статусное первым, финансовое вторым. Поэтому
                // финансовый комментарий создаётся раньше (он мутирует взнос), а в очередь попадает
                // позже.
                if (financeComment is { } finance)
                {
                    _ = ctx.EnqueueComment(finance);
                }
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
    /// Уведомление уходит только по старой заявке — как и до миграции. Комментарий к новой заявке
    /// создаётся, но молчит (<c>Silent</c>). Слать ли его — открытый вопрос ADR014, решение за ревью.
    /// </para>
    /// </remarks>
    public async Task<int> MoveToSecondRole(ClaimIdentification claimId, CharacterIdentification characterId, string secondRoleCommentText)
    {
        if (claimId.ProjectId != characterId.ProjectId)
        {
            throw new InvalidOperationException("Нельзя смешивать разные проекты в запросе");
        }

        var claim = await characterPropsService.CreateClaimAsync(
            characterId,
            // Игрок известен только из старой заявки, а она приезжает уже внутри фабрики.
            playerId: null,
            ClaimOperation.MoveToSecondRole,
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

                var newClaim = ctx.NewClaim(
                    ClaimStatus.Approved,
                    // Разрешение на чувствительные данные даёт игрок, а не мастер.
                    playerAllowedSensitiveData: false,
                    oldClaim.Player);

                // Вторую роль выдаёт мастер, значит заявка утверждена прямо сейчас и взноса не несёт.
                newClaim.MasterAcceptedDate = ctx.Now;
                newClaim.CurrentFee = 0;

                ctx.Character.ApprovedClaim = newClaim;

                // Уведомления об этом комментарии до миграции не было. Молчание сохранено —
                // см. открытый вопрос в ADR014.
                _ = ctx.AddComment(
                        ctx.Request.CommentText,
                        CommentExtraAction.SecondRole,
                        ClaimOperationType.MasterVisibleChange)
                    .Silent();

                // А вот по старой заявке уведомление уходит — как и до миграции.
                _ = ctx.AddComment(
                        oldClaim,
                        ctx.Request.CommentText,
                        CommentExtraAction.OutOfGame,
                        ClaimOperationType.MasterVisibleChange)
                    .Decorate(notification => notification with { AnotherCharacterId = characterId });

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

        var claim = await characterPropsService.CreateClaim(
            characterId,
            currentUserAccessor.UserIdentification,
            ClaimOperation.AddByPlayer,
            ProjectActiveRequirement.MustBeActive,
            (claimText, fields, sensitiveDataAllowed),
            ctx =>
            {
                var claim = ctx.NewClaim(
                    ClaimStatus.AddedByUser,
                    // Разрешение даёт игрок, и только если проект его вообще спрашивает.
                    playerAllowedSensitiveData: ctx.Request.sensitiveDataAllowed
                        && ctx.ProjectInfo.ProfileRequirementSettings.SensitiveDataRequired);

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
        await claimAutoApproveService.AutoApproveClaimIfNeeded(claimId);

        logger.LogInformation("Claim ({claimId}) was successfully send to character {characterId}", claimId, characterId);
        return claimId;
    }

    /// <summary>
    /// Комментарий к заявке — в том числе модерация финансовой операции из родительского
    /// комментария.
    /// </summary>
    /// <remarks>
    /// Единственная claim-операция с <see cref="ProjectActiveRequirement.AllowInactive"/>: по ADR014
    /// комментирование в архивном проекте остаётся разрешённым — обсуждение игры продолжается после
    /// её конца, и форма комментария в UI намеренно не спрятана.
    /// </remarks>
    public Task AddComment(ClaimIdentification claimId, int? parentCommentId, bool isVisibleToPlayer, string commentText, FinanceOperationAction financeAction)
        => characterPropsService.ChangeClaim(
            claimId,
            ClaimAccessRequirement.MasterOrPlayer,
            ProjectActiveRequirement.AllowInactive,
            (ParentCommentId: parentCommentId,
                IsVisibleToPlayer: isVisibleToPlayer,
                CommentText: commentText,
                FinanceAction: financeAction),
            ctx =>
            {
                var claimOperationType = ctx.Claim.PlayerUserId == ctx.CurrentUser.UserId
                    ? ClaimOperationType.PlayerChange
                    : ctx.Request.IsVisibleToPlayer
                        ? ClaimOperationType.MasterVisibleChange
                        : ClaimOperationType.MasterSecretChange;

                ctx.MarkDiscussed(ctx.Request.IsVisibleToPlayer);

                // Комментарии дискуссии грузит write-хэндл (Include(c => c.CommentDiscussion.Comments)),
                // иначе родителя было бы не найти и финансовая модерация тихо ломалась бы.
                var parentComment = ctx.Claim.CommentDiscussion.Comments
                    .SingleOrDefault(c => c.CommentId == ctx.Request.ParentCommentId);

                CommentExtraAction? extraAction = null;

                if (ctx.Request.FinanceAction != FinanceOperationAction.None)
                {
                    if (parentComment is null)
                    {
                        throw new InvalidOperationException("Requested to perform finance operation on parent comment, but there is no any");
                    }
                    extraAction = PerformFinanceOperation(ctx, ctx.Request.FinanceAction, parentComment);
                }

                var pending = ctx.AddComment(ctx.Request.CommentText, extraAction, claimOperationType);

                if (parentComment is not null)
                {
                    // Внутри — проверка «нельзя ответить на скрытый комментарий так, чтобы игрок
                    // ответ увидел».
                    _ = pending.SetParent(parentComment, claimOperationType);
                }
            });

    private static CommentExtraAction PerformFinanceOperation(
        ClaimMutationContext ctx,
        FinanceOperationAction financeAction,
        Comment parentComment)
    {
        var finance = parentComment.Finance;
        if (finance == null)
        {
            throw new InvalidOperationException();
        }

        if (!finance.RequireModeration)
        {
            throw new ValueAlreadySetException("Finance entry is already moderated.");
        }

        finance.RequestModerationAccess(ctx.CurrentUser.UserId);
        finance.Changed = ctx.Now;
        switch (financeAction)
        {
            case FinanceOperationAction.Approve:
                finance.State = FinanceOperationState.Approved;
                if (finance.OperationType == FinanceOperationType.PreferentialFeeRequest)
                {
                    ctx.Claim.PreferentialFeeUser = true;
                }
                ctx.Claim.UpdateClaimFeeIfRequired(finance.OperationDate, ctx.ProjectInfo);
                return CommentExtraAction.ApproveFinance;
            case FinanceOperationAction.Decline:
                finance.State = FinanceOperationState.Declined;
                return CommentExtraAction.RejectFinance;
            case FinanceOperationAction.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(financeAction), financeAction, null);
        }
    }

    /// <summary>
    /// Утверждение заявки живёт в <see cref="ClaimApprovalService"/> — им же пользуется автоприём
    /// (ADR014, §7), и общая зависимость через <see cref="IClaimApprovalService"/> разрывает цикл в DI.
    /// </summary>
    public Task ApproveByMaster(ClaimIdentification claimId, string commentText)
        => claimApprovalService.ApproveByMaster(claimId, commentText);

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

                var roomEmail = CommonClaimDecline(ctx);

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

                if (roomEmail is not null)
                {
                    // Порядок «сначала уведомления, потом письма легаси-канала» обеспечивает сервис.
                    ctx.AddLegacyEmail(emailService => emailService.Email(roomEmail));
                }
            });

    /// <summary>
    /// Деактивация персонажа вместе с отклонением заявки.
    /// </summary>
    /// <remarks>
    /// Вторая проверка прав внутри лямбды — намеренный escape-hatch (ADR014, §5): право зависит от
    /// аргумента операции (<c>deleteCharacter</c>), а требование доступа самой операции
    /// (<see cref="ClaimAccessRequirement.ApprovalDecline"/>) его не покрывает.
    /// </remarks>
    private static void DeleteCharacter(ClaimMutationContext ctx)
    {
        _ = ctx.ProjectInfo.RequestMasterAccess(ctx.CurrentUser, Permission.CanEditRoles);

        ctx.Character.DirectlyRelatedPlotElements.CleanLinksList();

        ctx.Character.IsActive = false;
        ctx.MarkChanged(ctx.Character);
    }

    /// <summary>
    /// Общая часть отклонения заявки на контексте мутации (ADR014). Возвращает письмо легаси-канала
    /// о выезде из комнаты, если заявка была поселена.
    /// </summary>
    private static LeaveRoomEmail? CommonClaimDecline(ClaimMutationContext ctx)
    {
        ctx.MarkCharacterChangedIfApproved();

        if (ctx.Claim.Character?.ApprovedClaim == ctx.Claim)
        {
            ctx.Claim.Character.ApprovedClaimId = null;
        }

        return ConsiderLeavingRoom(ctx);
    }

    /// <summary>
    /// То же, что <see cref="ConsiderLeavingRoom(Claim)"/>, но на контексте мутации: инициатор берётся
    /// из уже загруженного хэндла (лишнего запроса за текущим пользователем нет), а опустевшая заявка
    /// на поселение удаляется через тот же <c>DbContext</c>, а не через сырой <c>DbSet</c>.
    /// </summary>
    private static LeaveRoomEmail? ConsiderLeavingRoom(ClaimMutationContext ctx)
    {
        var claim = ctx.Claim;

        if (claim.AccommodationRequest is null)
        {
            return null;
        }

        LeaveRoomEmail? email = null;

        if (claim.AccommodationRequest.Accommodation is not null)
        {
            email = new LeaveRoomEmail()
            {
                Changed = [claim],
                Initiator = ctx.Initiator,
                ProjectName = claim.Project.ProjectName,
                Recipients = claim.AccommodationRequest.Accommodation.GetSubscriptions().ToList(),
                Room = claim.AccommodationRequest.Accommodation,
                Text = new MarkdownDbValue(),
            };
        }

        _ = claim.AccommodationRequest.Subjects.Remove(claim);
        if (claim.AccommodationRequest.Subjects.Count == 0)
        {
            ctx.RemoveEntity(claim.AccommodationRequest);
        }

        return email;
    }

    /// <summary>
    /// Отклоняет все приглашения к совместному проживанию, в которых участвует заявка, и ставит
    /// письмо остальным участникам в легаси-очередь.
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
                // Сама отклоняемая заявка письма о себе не получает.
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

        var email = new DeclineInviteEmail
        {
            Initiator = ctx.Initiator,
            ProjectName = ctx.Claim.Project.ProjectName,
            Recipients = [.. recipients.ToArray().GetInviteSubscriptions()],
            RecipientClaims = recipients,
            Text = new MarkdownDbValue(),
        };

        ctx.AddLegacyEmail(emailService => emailService.Email(email));
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

                var leaveEmail = ConsiderLeavingRoom(ctx);

                ctx.Claim.AccommodationRequest_Id = null;
                ctx.Claim.AccommodationRequest = null;

                ctx.AddEntity(new AccommodationRequest
                {
                    ProjectId = projectId,
                    AccommodationTypeId = acr.AccommodationTypeId,
                    IsAccepted = InviteState.Accepted,
                    Subjects = [ctx.Claim],
                });

                if (leaveEmail is not null)
                {
                    ctx.AddLegacyEmail(emailService => emailService.Email(leaveEmail));
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
                // Player cannot change accommodation type if already checked in

                if (ctx.Claim.AccommodationRequest?.AccommodationTypeId == roomTypeId)
                {
                    // Тип уже такой — операции нет, как и до миграции.
                    ctx.NothingChanged();
                    return ctx.Claim.AccommodationRequest;
                }

                // Типа поселения нет в ProjectInfo, поэтому он приезжает именованным загрузчиком —
                // через тот же DbContext, что и мутация. Нужен ради проверки существования.
                _ = await ctx.LoadAccommodationType(
                    new AccommodationTypeIdentification(ctx.ProjectInfo.ProjectId, roomTypeId));

                // TODO: восстановить отправку изменений полей, см. ADR014

                var leaveEmail = ConsiderLeavingRoom(ctx);

                // TODO: Just change accommodation type if this claim is the only occupant of previous room
                var accommodationRequest = new AccommodationRequest
                {
                    ProjectId = projectId,
                    Subjects = [ctx.Claim],
                    AccommodationTypeId = roomTypeId,
                    IsAccepted = InviteState.Accepted,
                };

                ctx.AddEntity(accommodationRequest);

                if (leaveEmail is not null)
                {
                    ctx.AddLegacyEmail(emailService => emailService.Email(leaveEmail));
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

                var roomEmail = CommonClaimDecline(ctx);

                _ = ctx.AddComment(
                    ctx.Request,
                    CommentExtraAction.DeclineByPlayer,
                    ClaimOperationType.PlayerChange);

                if (roomEmail is not null)
                {
                    // Порядок «сначала уведомления, потом письма легаси-канала» обеспечивает сервис.
                    ctx.AddLegacyEmail(emailService => emailService.Email(roomEmail));
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

                var userInfo = await UserRepository.GetRequiredUserInfo(
                    new UserIdentification(ctx.Claim.PlayerUserId));

                ClaimValidator.EnsureCanMoveClaim(
                    targetInfo,
                    new UserClaimInfo(ctx.Claim.GetId(), ctx.Claim.ClaimStatus),
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

    public Task SaveFieldsFromClaim(
        ClaimIdentification claimId,
        FieldLayerContainer fieldsToSet)
        => characterPropsService.ChangeClaim(
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
    public Task AllowSensitiveData(ClaimIdentification claimId)
        => characterPropsService.ChangeClaim(
            claimId,
            ClaimAccessRequirement.PlayerOnly,
            ProjectActiveRequirement.MustBeActive,
            claimId,
            ctx =>
            {
                ctx.Claim.PlayerAllowedSenstiveData = true;
                ctx.MarkDiscussed(isVisibleToPlayer: true);
            });

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
                    ctx.Request.CommentText ?? "",
                    CommentExtraAction.InvitationAcceptedByPlayer,
                    ClaimOperationType.PlayerChange);
            });

        // Автоприём — отдельная операция, идущая строго ПОСЛЕ принятия приглашения: реентерабельность
        // запрещена (ADR014, §7). Порядок сохранён — и раньше он шёл после рассылки уведомления.
        await claimAutoApproveService.AutoApproveClaimIfNeeded(claimId);
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

        // Права мастера и правила подачи проверяет сам props-сервис: ClaimOperation.AddByMaster
        // требует CanManageClaims и пропускает причины с MasterCanOverride — закрытый приём заявок
        // и незаполненные контакты игрока мастера не останавливают.
        var claim = await characterPropsService.CreateClaim(
            characterId,
            userId,
            ClaimOperation.AddByMaster,
            ProjectActiveRequirement.MustBeActive,
            (commentText, fields),
            ctx =>
            {
                var claim = ctx.NewClaim(
                    ClaimStatus.AddedByMaster,
                    // Мастер не может дать разрешение на чувствительные данные от имени игрока
                    playerAllowedSensitiveData: false);

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

