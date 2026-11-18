using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Web.Claims.Finance;

/// <summary>
/// Способ оплаты для игрока — строка модалки «Как оплатить…» на странице заявки.
/// </summary>
/// <param name="PaymentLink">Ссылка оплаты; <c>null</c> — ссылки нет, метод показывается с одним описанием.</param>
/// <param name="IsRecommended">Основной способ оплаты проекта — показываем первым, с пометкой «Рекомендуется».</param>
public record ClaimPaymentMethodView(
    string Name,
    Uri? PaymentLink,
    PaymentLinkKind LinkKind,
    string? Description,
    bool IsRecommended = false)
{
    public bool HasPaymentLink => PaymentLink is not null;
}
