namespace JoinRpg.DomainTypes.ProjectMetadata.Payments;

/// <summary>
/// Как игрок использует ссылку оплаты метода (<see cref="PaymentTypePlayerInfo.PaymentLink"/>).
/// </summary>
public enum PaymentLinkKind
{
    /// <summary>
    /// Простой переход по ссылке в новой вкладке.
    /// </summary>
    Redirect = 0,
}
