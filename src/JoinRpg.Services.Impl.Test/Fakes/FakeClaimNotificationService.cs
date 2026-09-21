using JoinRpg.Services.Impl.Claims;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Записывает уведомления по заявке (<see cref="IClaimNotificationService"/>) вместо реальной
/// постановки в очередь.
/// </summary>
/// <remarks>
/// Отдельные списки по типам нужны, чтобы тест проверял содержимое без приведения типов, а общий
/// <see cref="Sent"/> — чтобы проверять количество и <b>порядок</b> отправки. Порядок значим:
/// ADR014 требует рассылать накопленные уведомления в порядке добавления комментариев, уже после
/// <c>SaveChanges</c> — только тогда у уведомления заполнен <c>CommentId</c>.
/// </remarks>
internal sealed class FakeClaimNotificationService : IClaimNotificationService
{
    /// <summary>Все отправленные уведомления в порядке отправки.</summary>
    public List<IClaimNotification> Sent { get; } = [];

    public List<ClaimSimpleChangedNotification> SimpleChanged { get; } = [];

    public List<ClaimOnlinePaymentNotification> OnlinePayments { get; } = [];

    public Task SendNotification(ClaimSimpleChangedNotification model)
    {
        SimpleChanged.Add(model);
        Sent.Add(model);
        return Task.CompletedTask;
    }

    public Task SendNotification(ClaimOnlinePaymentNotification model)
    {
        OnlinePayments.Add(model);
        Sent.Add(model);
        return Task.CompletedTask;
    }
}
