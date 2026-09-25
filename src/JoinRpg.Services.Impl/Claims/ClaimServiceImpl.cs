using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
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
    FieldSaveHelper fieldSaveHelper,
    IAccommodationInviteService accommodationInviteService,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterInfoRepository characterInfoRepository,
    IProblemValidator<Claim> claimValidator,
    ILogger<CharacterServiceImpl> logger,
    IClaimNotificationService claimNotificationService,
    CommentHelper commentHelper,
    ICharacterPropsService characterPropsService,
    IImpersonateAccessor impersonateAccessor
    )
    : ClaimImplBase(unitOfWork, emailService, currentUserAccessor, projectMetadataRepository, commentHelper), IClaimService
{
    public async Task CheckInClaim(ClaimIdentification claimId, int money)
    {
        var (claim, projectInfo) = await LoadClaimAsMaster(claimId); //TODO Specific right
        claim.EnsureCanChangeStatus(ClaimStatus.CheckedIn);

        var validator = new ClaimCheckInValidator(claim, claimValidator, projectInfo);
        if (!validator.CanCheckInInPrinciple)
        {
            throw new ClaimWrongStatusException(claim.GetId(), claim.ClaimStatus);
        }

        ClaimSimpleChangedNotification? financeEmail = null;
        Comment? financeComment = null;
        if (money > 0)
        {
            var paymentType = projectInfo.ProjectFinanceSettings.GetCashPaymentType(currentUserAccessor.UserIdentification) ?? throw new JoinRpgInvalidUserException();

            (financeComment, financeEmail) = AcceptFeeImpl(".", Now, money, paymentType, claim, projectInfo);
        }
        else if (money < 0)
        {
            throw new InvalidOperationException();
        }

        if (!validator.CanCheckInNow)
        {
            throw new ClaimWrongStatusException(claim.GetId(), claim.ClaimStatus);
        }

        claim.ClaimStatus = ClaimStatus.CheckedIn;
        claim.CheckInDate = Now;
        MarkChanged(claim.Character);
        claim.Character.InGame = true;

        var (comment, email) = CommentHelper.CreateClaimCommentWithNotification("", claim, projectInfo, CommentExtraAction.CheckedIn, ClaimOperationType.MasterVisibleChange, Now);

        await UnitOfWork.SaveChangesAsync();

        await claimNotificationService.SendNotification(email.WithCommentId(comment.CommentId));

        if (financeEmail != null)
        {
            await claimNotificationService.SendNotification(financeEmail.WithCommentId(financeComment!.CommentId));
        }
    }

    public async Task<int> MoveToSecondRole(ClaimIdentification claimId, CharacterIdentification characterId, string secondRoleCommentText)
    {
        if (claimId.ProjectId != characterId.ProjectId)
        {
            throw new InvalidOperationException("Нельзя смешивать разные проекты в запросе");
        }
        var (oldClaim, projectInfo) = await LoadClaimAsMaster(claimId); //TODO Specific right
        oldClaim.EnsureStatus(ClaimStatus.CheckedIn);

        oldClaim.Character.InGame = false;
        MarkChanged(oldClaim.Character);

        var source = await CharactersRepository.GetCharacterAsync(characterId);

        MarkChanged(source);

        // TODO improve valitdation here
        //source.EnsureCanMoveClaim(oldClaim);

        var responsibleMaster = source.GetResponsibleMaster();
        // TODO последняя ручная сборка Claim: остальные создают заявку через ctx.NewClaim.
        // Уйдёт, когда MoveToSecondRole переедет на ICharacterPropsService.CreateClaim (ADR014).
        var claim = new Claim()
        {
            CharacterId = characterId.CharacterId,
            ProjectId = characterId.ProjectId,
            PlayerUserId = oldClaim.PlayerUserId,
            PlayerAcceptedDate = Now,
            CreateDate = Now,
            ClaimStatus = ClaimStatus.Approved,
            CurrentFee = 0,
            ResponsibleMasterUserId = responsibleMaster.UserId,
            ResponsibleMasterUser = responsibleMaster,
            LastUpdateDateTime = Now,
            MasterAcceptedDate = Now,
            CommentDiscussion =
            new CommentDiscussion() { CommentDiscussionId = -1, ProjectId = characterId.ProjectId },
        };

        claim.CommentDiscussion.Comments.Add(new Comment
        {
            CommentDiscussionId = -1,
            AuthorUserId = CurrentUserId,
            CommentText = new CommentText { Text = new MarkdownDbValue(secondRoleCommentText) },
            CreatedAt = Now,
            IsCommentByPlayer = false,
            IsVisibleToPlayer = true,
            ProjectId = characterId.ProjectId,
            LastEditTime = Now,
            ExtraAction = CommentExtraAction.SecondRole,
        });

        oldClaim.ClaimStatus = ClaimStatus.Approved;
        source.ApprovedClaim = claim;

        var (comment, email) = CommentHelper.CreateClaimCommentWithNotification(secondRoleCommentText, oldClaim, projectInfo, CommentExtraAction.OutOfGame, ClaimOperationType.MasterVisibleChange, Now);

        email = email with { AnotherCharacterId = characterId };


        _ = UnitOfWork.GetDbSet<Claim>().Add(claim);

        _ = fieldSaveHelper.SaveCharacterFields(CurrentUserId, claim, FieldLayerContainer.Empty(projectInfo), projectInfo);

        await UnitOfWork.SaveChangesAsync();

        await claimNotificationService.SendNotification(email.WithCommentId(comment.CommentId));

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
                var claim = ctx.NewClaim(ClaimStatus.AddedByUser, ctx.ResponsibleMasterByProjectRules());

                // Разрешение даёт игрок, и только если проект его вообще спрашивает.
                claim.PlayerAllowedSenstiveData = ctx.Request.sensitiveDataAllowed
                    && ctx.ProjectInfo.ProfileRequirementSettings.SensitiveDataRequired;

                _ = ctx.SaveFields(claim, ctx.Request.fields);

                //TODO добавить сюда измененные поля
                ctx.AddComment(
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

    public async Task AddComment(ClaimIdentification claimId, int? parentCommentId, bool isVisibleToPlayer, string commentText, FinanceOperationAction financeAction)
    {
        var (claim, projectInfo) = await LoadClaimAsMaster(claimId, Permission.None, ExtraAccessReason.Player);

        ClaimOperationType claimOperationType;

        if (claim.PlayerUserId == CurrentUserId)
        {
            claimOperationType = ClaimOperationType.PlayerChange;
        }
        else
        {
            claimOperationType = isVisibleToPlayer ? ClaimOperationType.MasterVisibleChange : ClaimOperationType.MasterSecretChange;
        }

        SetDiscussed(claim, isVisibleToPlayer);

        var parentComment = claim.CommentDiscussion.Comments.SingleOrDefault(c => c.CommentId == parentCommentId);

        CommentExtraAction? extraAction = null;

        if (financeAction != FinanceOperationAction.None)
        {
            if (parentComment is null)
            {
                throw new InvalidOperationException("Requested to perform finance operation on parent comment, but there is no any");
            }
            extraAction = PerformFinanceOperation(financeAction, parentComment, claim, projectInfo);
        }


        var result = CommentHelper.CreateClaimCommentWithNotification(commentText, claim, projectInfo, extraAction, claimOperationType, Now);

        if (parentComment is not null)
        {
            SetParentCommentAndCheck(result, parentComment, claimOperationType);
        }

        await UnitOfWork.SaveChangesAsync();

        await claimNotificationService.SendNotification(result.Item2.WithCommentId(result.Item1.CommentId));
    }

    private CommentExtraAction? PerformFinanceOperation(FinanceOperationAction financeAction,
      Comment parentComment, Claim claim, ProjectInfo projectInfo)
    {
        var finance = parentComment?.Finance;
        if (finance == null)
        {
            throw new InvalidOperationException();
        }

        if (!finance.RequireModeration)
        {
            throw new ValueAlreadySetException("Finance entry is already moderated.");
        }

        finance.RequestModerationAccess(CurrentUserId);
        finance.Changed = Now;
        switch (financeAction)
        {
            case FinanceOperationAction.Approve:
                finance.State = FinanceOperationState.Approved;
                if (finance.OperationType == FinanceOperationType.PreferentialFeeRequest)
                {
                    claim.PreferentialFeeUser = true;
                }
                claim.UpdateClaimFeeIfRequired(finance.OperationDate, projectInfo);
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
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(claimId.ProjectId);

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

                var roomEmail = CommonClaimDecline(ctx);

                if (ctx.Request.DeleteCharacter)
                {
                    if (!statusWasApproved)
                    {
                        throw new InvalidOperationException("Attempt to delete character, but it not exists");
                    }
                    DeleteCharacter(ctx);
                }

                await accommodationInviteService.DeclineAllClaimInvites(claimId);

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

    private async Task<LeaveRoomEmail?> CommonClaimDecline(Claim claim)
    {
        MarkCharacterChangedIfApproved(claim);


        if (claim.Character?.ApprovedClaim == claim)
        {
            claim.Character.ApprovedClaimId = null;
        }

        return await ConsiderLeavingRoom(claim);
    }

    private async Task<LeaveRoomEmail?> ConsiderLeavingRoom(Claim claim)
    {
        LeaveRoomEmail? email = null;

        if (claim.AccommodationRequest != null)
        {
            if (claim.AccommodationRequest.Accommodation != null)
            {
                email = new LeaveRoomEmail()
                {
                    Changed = new[] { claim },
                    Initiator = await GetCurrentUser(),
                    ProjectName = claim.Project.ProjectName,
                    Recipients = claim.AccommodationRequest.Accommodation.GetSubscriptions().ToList(),
                    Room = claim.AccommodationRequest.Accommodation,
                    Text = new MarkdownDbValue(),
                };
            }

            _ = claim.AccommodationRequest.Subjects.Remove(claim);
            if (!claim.AccommodationRequest.Subjects.Any())
            {
                _ = UnitOfWork.GetDbSet<AccommodationRequest>().Remove(claim.AccommodationRequest);
            }
        }

        return email;
    }

    /// <inheritdoc />
    public async Task<AccommodationRequest?> LeaveAccommodationGroupAsync(int projectId, int claimId)
    {
        var claim = await ClaimsRepository.GetClaim(new ClaimIdentification(projectId, claimId));

        claim = claim.RequestAccess(currentUserAccessor.UserIdentification,
            Permission.CanSetPlayersAccommodations,
            claim.ClaimStatus == ClaimStatus.Approved
                ? ExtraAccessReason.PlayerOrResponsible
                : ExtraAccessReason.None);

        var acr = claim.AccommodationRequest;
        if (acr is null)
        {
            return null;
        }
        if (acr.Subjects.Count == 1)
        {
            return acr;
        }

        var email = EmailHelpers.CreateFieldsEmailWithExtraData(
            claim,
            s => s.AccommodationChange,
            await GetCurrentUser(),
            [],
            "Тип поселения", //TODO[Localize]
            new PreviousAndNewValue("без поселения", acr.AccommodationType.Name)
            );
        var leaveEmail = await ConsiderLeavingRoom(claim);

        acr.Subjects.Remove(claim);
        claim.AccommodationRequest_Id = null;
        claim.AccommodationRequest = null;
        UnitOfWork.GetDbSet<AccommodationRequest>()
            .Add(
                new AccommodationRequest
                {
                    ProjectId = projectId,
                    AccommodationTypeId = acr.AccommodationTypeId,
                    IsAccepted = InviteState.Accepted,
                    Subjects = [claim]
                });

        await UnitOfWork.SaveChangesAsync();

        // Исправить потом отправку изменений
        //await EmailService.Email(email);
        if (leaveEmail is not null)
        {
            await EmailService.Email(leaveEmail);
        }

        return acr;
    }

    public async Task<AccommodationRequest> SetAccommodationType(int projectId,
        int claimId,
        int roomTypeId)
    {
        //todo set first state to Unanswered
        var claim = await ClaimsRepository.GetClaim(new ClaimIdentification(projectId, claimId)).ConfigureAwait(false);

        claim = claim.RequestAccess(currentUserAccessor.UserIdentification,
            Permission.CanSetPlayersAccommodations,
            claim?.ClaimStatus == ClaimStatus.Approved
                ? ExtraAccessReason.PlayerOrResponsible
                : ExtraAccessReason.None);

        // Player cannot change accommodation type if already checked in

        if (claim.AccommodationRequest?.AccommodationTypeId == roomTypeId)
        {
            return claim.AccommodationRequest;
        }

        var newType = await UnitOfWork.GetDbSet<ProjectAccommodationType>().FindAsync(roomTypeId)
            .ConfigureAwait(false);

        if (newType == null)
        {
            throw new JoinRpgEntityNotFoundException(roomTypeId,
                nameof(ProjectAccommodationType));
        }

        var email = EmailHelpers.CreateFieldsEmailWithExtraData(claim,
            s => s.AccommodationChange,
            await GetCurrentUser(),
            [],
            "Тип поселения", //TODO[Localize]
            new PreviousAndNewValue(newType.Name, claim.AccommodationRequest?.AccommodationType.Name)
            );


        var leaveEmail = await ConsiderLeavingRoom(claim);

        // TODO: Just change accommodation type if this claim is the only occupant of previous room
        var accommodationRequest = new AccommodationRequest
        {
            ProjectId = projectId,
            Subjects = [claim],
            AccommodationTypeId = roomTypeId,
            IsAccepted = InviteState.Accepted,
        };

        _ = UnitOfWork
            .GetDbSet<AccommodationRequest>()
            .Add(accommodationRequest);
        await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

        // Исправить потом отправку изменений
        // await EmailService.Email(email);
        if (leaveEmail != null)
        {
            await EmailService.Email(leaveEmail);
        }

        return accommodationRequest;
    }


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

                await accommodationInviteService.DeclineAllClaimInvites(claimId);

                var roomEmail = await CommonClaimDecline(ctx.Claim);

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

    public async Task MoveByMaster(ClaimIdentification claimId, string commentText, CharacterIdentification characterId)
    {
        var (claim, projectInfo) = await LoadClaimForApprovalDecline(claimId);
        var source = await CharactersRepository.GetCharacterAsync(characterId)
            ?? throw new JoinRpgEntityNotFoundException(characterId.CharacterId, nameof(Character));
        var userInfo = await UserRepository.GetRequiredUserInfo(new UserIdentification(claim.PlayerUserId));

        var oldCharacterId = claim.GetCharacterId(); // Сохраняем, так как он изменится

        ClaimValidator.EnsureCanMoveClaim(
            await characterInfoRepository.GetCharacterInfo(characterId),
            new UserClaimInfo(claim.GetId(), claim.ClaimStatus),
            userInfo);

        MarkCharacterChangedIfApproved(claim); // before move

        if (claim.Character != null && claim.IsApproved)
        {
            claim.Character.ApprovedClaim = null;
        }
        claim.CharacterId = characterId.CharacterId;
        claim.Character = source; //That fields is required later

        if (claim.IsApproved)
        {
            claim.Character.ApprovedClaim = claim;
        }

        MarkCharacterChangedIfApproved(claim); // after move

        var (comment, email) = CommentHelper.CreateClaimCommentWithNotification(commentText, claim, projectInfo, CommentExtraAction.MoveByMaster, ClaimOperationType.MasterVisibleChange, Now);

        email = email with { AnotherCharacterId = oldCharacterId };


        await UnitOfWork.SaveChangesAsync();
        await claimNotificationService.SendNotification(email.WithCommentId(comment.CommentId));
    }

    public async Task UpdateReadCommentWatermark(int projectId, int commentDiscussionId, int maxCommentId)
    {
        var watermarks =
          UnitOfWork.GetDbSet<ReadCommentWatermark>()
            .Where(w => w.CommentDiscussionId == commentDiscussionId && w.UserId == CurrentUserId)
            .OrderByDescending(wm => wm.ReadCommentWatermarkId)
            .ToList();

        //Sometimes watermarks can duplicate. If so, let's remove them.
        foreach (var wm in watermarks.Skip(1))
        {
            _ = UnitOfWork.GetDbSet<ReadCommentWatermark>().Remove(wm);
        }

        var watermark = watermarks.FirstOrDefault();

        if (watermark == null)
        {
            watermark = new ReadCommentWatermark()
            {
                CommentDiscussionId = commentDiscussionId,
                ProjectId = projectId,
                UserId = CurrentUserId,
            };
            _ = UnitOfWork.GetDbSet<ReadCommentWatermark>().Add(watermark);
        }

        if (watermark.CommentId > maxCommentId)
        {
            return;
        }
        watermark.CommentId = maxCommentId;
        await UnitOfWork.SaveChangesAsync();
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
        var (claim, projectInfo) = await LoadClaimAsMaster(claimId, Permission.None, ExtraAccessReason.Player);

        var updatedFields = fieldSaveHelper.SaveCharacterFields(CurrentUserId, claim, fieldsToSet, projectInfo);
        if (updatedFields.Any(f => f.Field.BoundTo == FieldBoundTo.Character) && claim.Character != null)
        {
            MarkChanged(claim.Character);
        }
        var user = await GetCurrentUser();
        var email = EmailHelpers.CreateFieldsEmail(claim, s => s.FieldChange, user, updatedFields);

        await UnitOfWork.SaveChangesAsync();

        // Исправить потом отправку изменений
        // await EmailService.Email(email);
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

    private void MarkCharacterChangedIfApproved(Claim claim)
    {
        if (claim.ClaimStatus == ClaimStatus.Approved && claim.Character != null)
        {
            MarkChanged(claim.Character);
        }
    }

    private void SetDiscussed(Claim claim, bool isVisibleToPlayer)
    {
        claim.LastUpdateDateTime = Now;
        if (claim.ClaimStatus == ClaimStatus.AddedByMaster && CurrentUserId == claim.PlayerUserId)
        {
            claim.ClaimStatus = ClaimStatus.Discussed;
        }

        if (claim.ClaimStatus == ClaimStatus.AddedByUser && CurrentUserId != claim.PlayerUserId && isVisibleToPlayer)
        {
            claim.ClaimStatus = ClaimStatus.Discussed;
        }
    }

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
            await UnitOfWork.SaveChangesAsync();
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

    public async Task<ClaimIdentification> SystemEnsureClaim(ProjectIdentification donateProjectId)
    {
        var claims = await ClaimsRepository.GetClaimsForPlayer(donateProjectId, currentUserAccessor.UserIdentification, ClaimStatusSpec.Any);
        var claim = claims.TrySelectSingleClaim() ?? claims.FirstOrDefault();
        if (claim != null)
        {
            //TODO восстановить заявку, если она была отозвана или отклонена
            return claim.GetId();
        }
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(donateProjectId);
        if (projectInfo.ClaimSettings.DefaultTemplate is null)
        {
            logger.LogError("Некорректно настроен проект донатов {donateProjectId}", donateProjectId);
            throw new JoinRpgProjectMisconfiguredException(donateProjectId, "У проекта должен быть шаблон по умолчанию");
        }
        return await AddClaimFromUser(projectInfo.ClaimSettings.DefaultTemplate, claimText: "", fields: FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
    }

    public static void SetParentCommentAndCheck((Comment, ClaimSimpleChangedNotification) result, Comment parentComment, ClaimOperationType claimOperationType)
    {
        if (claimOperationType != ClaimOperationType.MasterSecretChange && parentComment.IsVisibleToPlayer == false)
        {
            throw new EntityWrongStatusException(parentComment); // Нельзя ответить на скрытый комментарий так, чтобы игрок видел
        }

        result.Item1.Parent = parentComment;
        result.Item2 = result.Item2 with
        {
            ParentCommentAuthor = parentComment.Author.ToUserInfoHeader(),
            PaymentOwner = parentComment?.Finance?.PaymentType?.User?.ToUserInfoHeader(),
        };
    }

    private async Task<(Claim, ProjectInfo)> LoadClaimForApprovalDecline(ClaimIdentification claimId)
    {
        return await LoadClaimAsMaster(claimId, Permission.CanManageClaims, ExtraAccessReason.ResponsibleMaster);
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
                var claim = ctx.NewClaim(ClaimStatus.AddedByMaster, ctx.ResponsibleMasterByProjectRules());

                _ = ctx.SaveFields(claim, ctx.Request.fields);

                // Комментарий о приглашении
                ctx.AddComment(
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

