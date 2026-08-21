namespace JoinRpg.Web.Accommodation;

/// <summary>
/// Одна строка выпадающего списка «кого пригласить». Это либо игрок, ещё не выбравший тип
/// проживания, либо целая сложившаяся группа соседей.
/// </summary>
/// <param name="TargetId">Кого приглашаем</param>
/// <param name="Text">Что видно в списке: имя игрока либо перечисление имён группы</param>
/// <param name="ExtraSearch">Дополнительные слова для поиска по списку (имена персонажей)</param>
/// <param name="Subtext">Поясняющая подпись под строкой</param>
public record AccommodationInviteTargetViewModel(
    AccommodationTargetIdentification TargetId,
    string Text,
    string ExtraSearch,
    string Subtext);

/// <summary>
/// Всё, что нужно контролу приглашения, одним согласованным снимком.
/// </summary>
/// <param name="SenderRequestId">Заявка на проживание приглашающего. <c>null</c> — тип проживания не выбран</param>
/// <param name="RoomFreeSpace">Сколько ещё человек влезет к приглашающему</param>
/// <param name="Targets">Кого можно пригласить. Пустой список — приглашать некого</param>
public record AccommodationInviteTargetsViewModel(
    AccommodationRequestIdentification? SenderRequestId,
    int RoomFreeSpace,
    IReadOnlyCollection<AccommodationInviteTargetViewModel> Targets);

/// <summary>
/// С какой стороны смотрим на приглашение.
/// </summary>
public enum InviteDirection
{
    /// <summary>Приглашения, полученные этой заявкой</summary>
    Incoming,

    /// <summary>Приглашения, отправленные этой заявкой</summary>
    Outgoing,
}

/// <summary>
/// Приглашение в списке полученных или отправленных.
/// </summary>
/// <param name="InviteId">Идентификатор приглашения</param>
/// <param name="Counterparty">Вторая сторона: пригласивший для полученных, приглашённый для отправленных</param>
/// <param name="State">Текущее состояние приглашения</param>
public record AccommodationInviteViewModel(
    AccommodationInviteIdentification InviteId,
    UserLinkViewModel Counterparty,
    InviteState State);

/// <summary>
/// Операции с приглашениями к совместному проживанию.
/// </summary>
public interface IAccommodationInviteClient
{
    /// <summary>Получить список тех, кого заявка <paramref name="claimId"/> может пригласить</summary>
    Task<AccommodationInviteTargetsViewModel> GetInviteTargets(ClaimIdentification claimId);

    /// <summary>Отправить приглашение. Бросает исключение, если пригласить нельзя</summary>
    Task CreateInvite(ClaimIdentification claimId, AccommodationTargetIdentification target);

    /// <summary>Полученные либо отправленные приглашения заявки, кроме уже принятых</summary>
    Task<IReadOnlyCollection<AccommodationInviteViewModel>> GetInvites(
        ClaimIdentification claimId,
        InviteDirection direction);

    /// <summary>Принять полученное приглашение</summary>
    Task AcceptInvite(AccommodationInviteIdentification inviteId);

    /// <summary>Отклонить полученное приглашение</summary>
    Task DeclineInvite(AccommodationInviteIdentification inviteId);

    /// <summary>Отозвать отправленное приглашение</summary>
    Task CancelInvite(AccommodationInviteIdentification inviteId);
}
