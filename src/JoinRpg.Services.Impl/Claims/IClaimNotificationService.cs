namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Ставит в очередь уведомления по заявке (комментарии, смена статуса, финансовые операции).
/// Вызывать ПОСЛЕ сохранения комментария — только тогда известен CommentId,
/// на который ссылается уведомление.
/// </summary>
internal interface IClaimNotificationService
{
    /// <summary>
    /// Уведомление об изменении заявки (комментарий, смена статуса и т.п.).
    /// </summary>
    Task SendNotification(ClaimSimpleChangedNotification model);

    /// <summary>
    /// Уведомление об онлайн-оплате по заявке.
    /// </summary>
    Task SendNotification(ClaimOnlinePaymentNotification model);
}
