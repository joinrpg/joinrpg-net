using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel;
using JoinRpg.Domain.CharacterFields;
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
    /// Группа проживающих другой заявки вместе с составом, либо <c>null</c>, если та ещё не выбрала
    /// тип проживания. Своя группа у заявки уже на руках — <c>Claim.AccommodationRequest</c>.
    /// </summary>
    public Task<AccommodationRequest?> LoadAccommodationGroupForClaim(ClaimIdentification claimId)
        => Scope.LoadAccommodationGroupForClaim(claimId);

    /// <summary>
    /// Группа проживающих по идентификатору вместе с составом, либо <c>null</c>, если такой группы
    /// в проекте нет.
    /// </summary>
    public Task<AccommodationRequest?> LoadAccommodationGroup(AccommodationRequestIdentification groupId)
        => Scope.LoadAccommodationGroup(groupId);

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
