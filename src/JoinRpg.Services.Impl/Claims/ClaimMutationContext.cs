using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Impl.Accommodation;
using JoinRpg.Services.Impl.Characters;

namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Контекст изменения заявки. Мутация заявки — это мутация агрегата персонажа, дополнительно
/// называющая конкретную заявку (ADR014), поэтому контекст наследует
/// <see cref="CharacterMutationContext"/> и даёт доступ и к персонажу, и к его снимку.
/// </summary>
/// <param name="Claim">Трекаемая EF-сущность заявки; её и нужно мутировать.</param>
/// <param name="ClaimSnapshot">
/// Доменный снимок заявки в составе персонажа строго ДО изменения (ADR021). Его персонаж и есть
/// <see cref="CharacterMutationContext.CharacterInfo"/> — отдельным параметром снимок персонажа не
/// передаётся, чтобы их нельзя было передать несогласованными.
/// </param>
internal abstract record ClaimMutationContext(
    Claim Claim,
    ClaimInCharacter ClaimSnapshot,
    Character Character,
    ProjectInfo ProjectInfo,
    DateTime Now,
    ICurrentUserAccessor CurrentUser,
    IAggregateMutationScope Scope,
    FieldSaveHelper FieldSaveHelper,
    CommentHelper CommentHelper)
    : CharacterMutationContext(Character, ClaimSnapshot.Character, ProjectInfo, Now, CurrentUser, Scope, FieldSaveHelper)
{
    /// <summary>Доменный снимок самой заявки — сокращение для <c>ClaimSnapshot.Claim</c>.</summary>
    public CharacterClaimInfo CharacterClaimInfo => ClaimSnapshot.Claim;

    /// <summary>
    /// Снимок заявки вместе с профилем игрока — для правил, которым нужен профиль (проблемы
    /// заявки, регистрация, перенос).
    /// </summary>
    /// <remarks>
    /// Профиль в хэндл агрегата не входит (ADR014 держит его узким), поэтому догружается здесь, по
    /// требованию. Репозиторий приходит параметром: сервисы берут репозитории из
    /// <c>IUnitOfWork</c>, а не из DI.
    /// </remarks>
    public async Task<ClaimInfo> LoadClaimInfo(IUserRepository userRepository)
        => new(ClaimSnapshot, await userRepository.GetRequiredUserInfo(CharacterClaimInfo.PlayerId));

    /// <summary>
    /// Сюжеты, привязанные напрямую к персонажу, трекаемые тем же <c>DbContext</c>. Нужны созданию
    /// персонажа из слота.
    /// </summary>
    public Task<IReadOnlyCollection<PlotElement>> LoadDirectPlotsForCharacter(CharacterIdentification characterId)
        => Scope.LoadDirectPlotsForCharacter(characterId);

    /// <summary>
    /// Приглашения к совместному проживанию, в которых участвует эта заявка, — трекаемые тем же
    /// <c>DbContext</c>, поэтому их изменение уедет в то же единственное сохранение.
    /// </summary>
    public Task<IReadOnlyCollection<AccommodationInvite>> LoadInvitesForClaim()
        => Scope.LoadInvitesForClaim(CharacterClaimInfo.ClaimId);

    /// <summary>
    /// То же для другой заявки того же проекта: приём приглашения снимает повисшие приглашения со
    /// всех, кто переезжает вместе с принявшим.
    /// </summary>
    public Task<IReadOnlyCollection<AccommodationInvite>> LoadInvitesForClaim(ClaimIdentification claimId)
        => Scope.LoadInvitesForClaim(claimId);

    /// <summary>
    /// Приглашение к совместному проживанию по идентификатору — трекаемое, вместе с заявками обеих
    /// сторон.
    /// </summary>
    /// <exception cref="JoinRpgEntityNotFoundException">Приглашения в этом проекте нет.</exception>
    public Task<AccommodationInvite> LoadInvite(AccommodationInviteIdentification inviteId)
        => Scope.LoadInvite(inviteId);

    /// <summary>
    /// Трекаемая группа проживающих другой заявки вместе с составом, либо <c>null</c>, если та ещё
    /// не выбрала тип проживания.
    /// </summary>
    /// <remarks>
    /// Решения о группах по ней не принимаются — для этого есть снимки (ADR022 §4):
    /// <see cref="LoadOtherClaimAccommodation"/> и план поселения. Потребителей не осталось, загрузчик
    /// удаляется следующим шагом ADR022.
    /// </remarks>
    public Task<AccommodationRequest?> LoadAccommodationGroupForClaim(ClaimIdentification claimId)
        => Scope.LoadAccommodationGroupForClaim(claimId);

    /// <summary>
    /// Трекаемая группа проживающих по идентификатору, либо <c>null</c>, если такой группы в проекте
    /// нет.
    /// </summary>
    /// <remarks>
    /// Не источник решений (ADR022 §4): приглашение берёт отсюда только существование и тип группы,
    /// которой нет в плане приглашающего, а решает по плану её типа.
    /// </remarks>
    public Task<AccommodationRequest?> LoadAccommodationGroup(AccommodationRequestIdentification groupId)
        => Scope.LoadAccommodationGroup(groupId);

    /// <summary>
    /// План поселения категории, из которой селится тип проживания, — снимок ДО изменения на
    /// <c>ProjectInfo</c> этой мутации. По нему, а не по навигациям
    /// трекаемых сущностей, принимаются решения о группах (ADR022 §4).
    /// </summary>
    /// <exception cref="AccommodationTypeNotFoundException">Типа нет в этом проекте.</exception>
    public Task<RoomCategoryPlan> LoadRoomCategoryPlan(AccommodationTypeIdentification typeId)
        => Scope.LoadRoomCategoryPlan(typeId);

    /// <summary>
    /// Снимок группы проживающих этой заявки ДО изменения вместе с планом, из которого он взят, либо
    /// <c>null</c>, если заявка ещё не выбрала тип проживания. В последнем случае в БД не ходит.
    /// </summary>
    /// <remarks>
    /// Тип и ссылка на группу берутся из <see cref="CharacterClaimInfo"/>: его конструктор держит
    /// инвариант «тип задан ⇔ ссылка указывает на группу» (ADR022 §2), поэтому группа с заданным
    /// типом в плане этого типа обязана найтись.
    /// </remarks>
    public async Task<(RoomCategoryPlan Plan, AccommodationGroupInfo Group)?> LoadOwnAccommodationGroup()
    {
        if (CharacterClaimInfo.AccommodationTypeId is not { } typeId)
        {
            return null;
        }

        var plan = await LoadRoomCategoryPlan(typeId);
        var group = plan.GetGroupOrDefault(CharacterClaimInfo.AccommodationGroupId)
            ?? throw new InvalidOperationException(
                $"Claim {CharacterClaimInfo.ClaimId} has accommodation type {typeId}, but no accommodation group");
        return (plan, group);
    }

    /// <summary>
    /// Сохраняет поля <b>через заявку</b>, а не через персонажа хэндла. Разница не косметическая:
    /// стратегия сохранения выбирается по <c>Claim.IsApproved</c>, а сам персонаж берётся из
    /// заявки — при утверждении заявки на слот она к этому моменту уже переехала на только что
    /// созданного персонажа, которого в хэндле нет.
    /// </summary>
    public IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveFields(FieldLayerContainer fieldsToSet)
        => SaveFieldsCore(Claim, fieldsToSet);

    /// <summary>
    /// Явный выход за границу агрегата: другой персонаж того же проекта — трекаемая сущность вместе
    /// со своим доменным снимком. Операции над двумя персонажами (перенос, восстановление, вторая
    /// роль) пересекают границу только так, а не скрытой ленивой навигацией (ADR014).
    /// </summary>
    public Task<(Character Entity, CharacterInfo Info)> LoadOtherCharacter(CharacterIdentification characterId)
        => Scope.LoadOtherCharacter(characterId);

    /// <summary>
    /// То же для другой заявки того же проекта. Промах по идентификатору — ошибка: заявка приходит
    /// параметром операции, и тихо считать её отсутствующей значило бы работать с чужими данными.
    /// </summary>
    /// <exception cref="JoinRpgEntityNotFoundException">Заявки в этом проекте нет.</exception>
    public Task<Claim> LoadOtherClaim(ClaimIdentification claimId)
        => Scope.LoadOtherClaim(claimId);

    /// <summary>
    /// Другая заявка того же проекта — трекаемая сущность вместе с её ссылкой на группу
    /// проживающих: тип и группа ровно в том виде, в каком их несёт
    /// <see cref="CharacterClaimInfo"/> (ADR022 §2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ссылка строится из того, что приезжает с заявкой одним запросом, — её внешнего ключа и
    /// включённой строки группы (<see cref="LoadOtherClaim"/> делает <c>Include</c>), — по тем же
    /// правилам, что у маппера снимка: заявка без группы ссылается сама на себя. Полный снимок через
    /// агрегат персонажа (<see cref="LoadOtherCharacter"/>) стоил бы ещё загрузки персонажа и
    /// проекции его агрегата, а приглашению из него нужны только эти два поля.
    /// </para>
    /// <para>
    /// Решения о самой группе по-прежнему принимаются по снимку — по плану поселения её типа
    /// (ADR022 §4); навигация группы здесь служит только источником её типа и «ручкой» для записи.
    /// </para>
    /// </remarks>
    /// <exception cref="JoinRpgEntityNotFoundException">Заявки в этом проекте нет.</exception>
    public async Task<(Claim Entity, AccommodationTypeIdentification? TypeId, AccommodationGroupIdentification GroupId)>
        LoadOtherClaimAccommodation(ClaimIdentification claimId)
    {
        var claim = await LoadOtherClaim(claimId);

        if (claim.AccommodationRequest_Id is not { } requestId)
        {
            return (claim, null, AccommodationGroupIdentification.From(claimId));
        }

        var group = claim.AccommodationRequest;
        if (group is null || group.Id != requestId)
        {
            throw new InvalidOperationException(
                $"Accommodation group {requestId} of claim {claimId} is not loaded with the claim");
        }

        return (
            claim,
            new AccommodationTypeIdentification(claimId.ProjectId, group.AccommodationTypeId),
            AccommodationGroupIdentification.From(new AccommodationRequestIdentification(claimId.ProjectId, requestId)));
    }

    /// <summary>
    /// Комментарии, созданные операцией, в порядке создания. Уведомления по ним сервис отправит
    /// после сохранения — см. <see cref="PendingComment"/>.
    /// </summary>
    internal List<PendingComment> PendingComments { get; } = [];

    /// <summary>
    /// Уведомления о выезде из комнаты, поставленные в очередь операцией над заявкой. Отправляются
    /// после всех уведомлений по комментариям — в том же порядке, в каком это происходило до
    /// миграции на <c>INotificationService</c>.
    /// </summary>
    internal List<RoomOccupancyNotification> RoomNotifications { get; } = [];

    /// <summary>
    /// Уведомления о снятии приглашений к совместному проживанию. Отдельный список, а не общий с
    /// <see cref="RoomNotifications"/>: типы уведомлений разные, а взаимный порядок между «о
    /// комнате» и «о приглашении» ничего не значит — это сообщения о разных событиях.
    /// </summary>
    internal List<AccommodationInviteNotification> InviteNotifications { get; } = [];

    /// <summary>
    /// Добавляет комментарий к этой заявке и ставит уведомление по нему в очередь.
    /// </summary>
    public PendingComment AddComment(
        string commentText,
        CommentExtraAction? extraAction,
        ClaimOperationType operationType)
        => AddComment(Claim, commentText, extraAction, operationType);

    /// <summary>
    /// То же для другой заявки того же игрока — нужно автоотклонению при
    /// <c>StrictlyOneCharacter</c>.
    /// </summary>
    public PendingComment AddComment(
        Claim claim,
        string commentText,
        CommentExtraAction? extraAction,
        ClaimOperationType operationType)
    {
        var (comment, notification) = CommentHelper.CreateClaimCommentWithNotification(
            commentText, claim, ProjectInfo, extraAction, operationType, Now);

        var pending = new PendingComment(comment, notification);
        PendingComments.Add(pending);

        return pending;
    }

    /// <summary>
    /// Запрещает операции, датированные будущим.
    /// </summary>
    public void CheckOperationDate(DateTime operationDate)
        => OperationDateValidation.CheckOperationDate(operationDate, Now);

    /// <summary>
    /// Ставит уведомление о комнате в очередь. Отправится после сохранения и после уведомлений
    /// по комментариям.
    /// </summary>
    public void AddRoomNotification(RoomOccupancyNotification notification)
        => RoomNotifications.Add(notification);

    /// <summary>
    /// Ставит уведомление о снятии приглашений в очередь. Отправится после сохранения и после
    /// уведомлений по комментариям.
    /// </summary>
    public void AddInviteNotification(AccommodationInviteNotification notification)
        => InviteNotifications.Add(notification);

    /// <summary>
    /// Помечает персонажа изменённым, если заявка утверждена. Перенос
    /// <c>ClaimServiceImpl.MarkCharacterChangedIfApproved</c>.
    /// </summary>
    public void MarkCharacterChangedIfApproved()
    {
        if (Claim.ClaimStatus == ClaimStatus.Approved)
        {
            // Персонаж берётся из заявки, а не из хэндла: при утверждении заявки на слот заявка
            // к этому моменту уже переехала на только что созданного персонажа.
            this.MarkChanged(Claim.Character ?? Character);
        }
    }
}

/// <summary>
/// Контекст изменения заявки с типизированными аргументами операции (<see cref="Request"/>).
/// </summary>
internal sealed record ClaimMutationContext<TArgs>(
    Claim Claim,
    ClaimInCharacter ClaimSnapshot,
    Character Character,
    ProjectInfo ProjectInfo,
    DateTime Now,
    ICurrentUserAccessor CurrentUser,
    IAggregateMutationScope Scope,
    FieldSaveHelper FieldSaveHelper,
    CommentHelper CommentHelper,
    TArgs Request)
    : ClaimMutationContext(Claim, ClaimSnapshot, Character, ProjectInfo, Now, CurrentUser,
        Scope, FieldSaveHelper, CommentHelper);
