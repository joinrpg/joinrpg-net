namespace JoinRpg.DomainTypes.ProjectMetadata.Payments;

/// <summary>
/// Что игрок видит о способе оплаты: название, ссылка на страницу оплаты и описание.
/// Большие поля — в <see cref="ProjectMetadata.ProjectDetails"/>, не в ProjectInfo (#5993).
/// </summary>
/// <param name="PaymentLink">
/// Ссылка оплаты. В EF-сущности хранится строкой (EF6 не мапит Uri), в доменный снимок попадает
/// типизированной: запись валидируется в <c>ProjectFinanceSettingsService</c> (абсолютный
/// http/https), а чтение через <c>Uri.TryCreate</c> переживает правку базы руками.
/// </param>
public record PaymentTypePlayerInfo(
    PaymentTypeIdentification PaymentTypeId,
    string Name,
    Uri? PaymentLink,
    PaymentLinkKind LinkKind,
    string? Description);
