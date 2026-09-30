namespace JoinRpg.Services.Impl.Accommodation;

/// <summary>
/// Что именно случилось с составом жителей комнаты. Один тип уведомления на четыре случая, а не
/// четыре класса: тексты отличаются только формулировкой, а получатели и заголовок считаются
/// одинаково.
/// </summary>
internal enum RoomOccupancyChangeKind
{
    /// <summary>Группы вселились в комнату</summary>
    Occupied,

    /// <summary>Группы выселены из комнаты мастером</summary>
    Evicted,

    /// <summary>Игрок выехал, потому что его заявку отозвали или отклонили</summary>
    ClaimDeclined,

    /// <summary>
    /// Игрок сам выехал из комнаты — сменил тип проживания или вышел из группы соседей.
    /// </summary>
    /// <remarks>
    /// Легаси-канал в этом случае отправлял текст про отклонённую заявку: <c>LeaveRoomEmail</c> был
    /// один на все четыре вызова <c>ConsiderLeavingRoom</c>. Текст не видел никто (тело письма было
    /// закомментировано), поэтому расхождение исправлено сразу при переносе.
    /// </remarks>
    LeftRoom,
}

/// <summary>
/// Уведомление об изменении состава жителей комнаты.
/// </summary>
/// <remarks>
/// Сущностей DataModel здесь нет намеренно: на входе типизированные идентификаторы, имена игроков
/// и название типа проживания сервис уведомлений добирает сам — по заявкам и по метаданным проекта.
/// </remarks>
/// <param name="RoomId">Комната, состав которой изменился.</param>
/// <param name="RoomName">
/// Название (номер) комнаты. Это оперативные данные, в <c>ProjectInfo</c> их нет (ADR015), поэтому
/// они приходят от вызывающего, у которого комната под рукой.
/// </param>
/// <param name="AccommodationTypeId">
/// Тип проживания комнаты — по нему сервис возьмёт название из метаданных, без обращения к
/// ленивой навигации EF.
/// </param>
/// <param name="Initiator">Кто выполнил операцию.</param>
/// <param name="Changed">Заявки, которых операция сдвинула: вселила, выселила или выписала.</param>
/// <param name="Remaining">
/// Заявки, которые после операции остались в комнате (для заселения — те, кто уже там жил).
/// </param>
internal record RoomOccupancyNotification(
    AccommodationRoomIdentification RoomId,
    string RoomName,
    AccommodationTypeIdentification AccommodationTypeId,
    UserInfoHeader Initiator,
    IReadOnlyCollection<ClaimIdentification> Changed,
    IReadOnlyCollection<ClaimIdentification> Remaining,
    RoomOccupancyChangeKind Kind);

/// <summary>Что случилось с приглашением к совместному проживанию.</summary>
internal enum InviteChangeKind
{
    /// <summary>Приглашение отправлено</summary>
    Created,

    /// <summary>Приглашение принято</summary>
    Accepted,

    /// <summary>
    /// Приглашение отменено приглашающим, отклонено приглашённым или снято вместе с отзывом заявки.
    /// Легаси-канал все три случая тоже отправлял одним письмом (<c>DeclineInviteEmail</c>).
    /// </summary>
    Cancelled,
}

/// <summary>
/// Уведомление об изменении приглашения к совместному проживанию.
/// </summary>
/// <param name="RecipientClaims">
/// Заявки, чьи подписчики получат уведомление. Уведомление ставится по одному на заявку: в нём
/// ссылка на страницу этой заявки, где приглашениями и управляют, а у разных получателей заявки
/// разные. Легаси-канал по той же причине отправлял по письму на получателя.
/// </param>
/// <remarks>
/// Проекта в модели намеренно нет: он выводится из заявок получателей. Отдельным полем он был бы
/// вторым источником правды, и он бы разошёлся — операции над приглашением не проверяют, что
/// <c>AccommodationInviteIdentification.ProjectId</c> из запроса совпадает с проектом самого
/// приглашения, так что название проекта в уведомлении оказалось бы управляемым извне.
/// </remarks>
internal record AccommodationInviteNotification(
    IReadOnlyCollection<ClaimIdentification> RecipientClaims,
    UserInfoHeader Initiator,
    InviteChangeKind Kind);
