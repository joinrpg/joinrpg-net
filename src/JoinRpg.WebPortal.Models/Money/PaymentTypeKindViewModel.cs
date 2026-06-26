using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Web.Models;

/// <summary>
/// Map of <see cref="PaymentTypeKind"/>
/// </summary>
public enum PaymentTypeKindViewModel
{
    Custom = PaymentTypeKind.Custom,

    [Display(Name = "Наличными")]
    Cash = PaymentTypeKind.Cash,

    [Display(Name = "Онлайн")]
    Online = PaymentTypeKind.Online,

    [Display(Name = "Подписка")]
    OnlineSubscription = PaymentTypeKind.OnlineSubscription,
}
