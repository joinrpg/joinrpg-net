using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.Notifications;

namespace JoinRpg.Web.AdminTools.Notifications;

public interface INotificationLogClient
{
    /// <summary>
    /// Последние уведомления, новые сверху.
    /// </summary>
    /// <param name="recipient">Только уведомления этому пользователю; null — всем.</param>
    /// <param name="channel">Только уведомления, отправляемые по этому каналу; null — все каналы.</param>
    Task<NotificationLogViewModel> GetLog(UserIdentification? recipient, NotificationChannel? channel);
}

/// <param name="RecipientId">Id из фильтра по получателю как его запросили — даже если такого пользователя уже нет
/// (удалён, слит), уведомления на этот id остались. null — фильтра нет.</param>
/// <param name="Recipient">Ссылка на пользователя из фильтра.</param>
public record NotificationLogViewModel(
    UserIdentification? RecipientId,
    UserLinkViewModel? Recipient,
    NotificationChannel? Channel,
    int Limit,
    IReadOnlyCollection<NotificationLogItemViewModel> Items);

/// <param name="BodyHtml">Тело, уже переведённое из Markdown в HTML и очищенное санитайзером.</param>
public record NotificationLogItemViewModel(
    NotificationId Id,
    DateTimeOffset CreatedAt,
    UserIdentification RecipientId,
    UserLinkViewModel Recipient,
    UserLinkViewModel Initiator,
    string Header,
    string BodyHtml,
    IReadOnlyCollection<NotificationDeliveryViewModel> Channels);

/// <param name="Address">Адрес в канале как он лежит в базе.</param>
public record NotificationDeliveryViewModel(
    NotificationChannel Channel,
    string Address,
    NotificationMessageStatus Status,
    int Attempts,
    DateTimeOffset SendAfter);
