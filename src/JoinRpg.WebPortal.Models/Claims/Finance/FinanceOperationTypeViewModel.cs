using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models;

/// <summary>
/// Map of <see cref="FinanceOperationType"/> enum
/// </summary>
/// <remarks>
/// Description template parameters:
/// 0 -- operation type name
/// 1 -- payment type name (or null)
/// </remarks>
public enum FinanceOperationTypeViewModel
{
    [Display(Name = "Изменение взноса")]
    [Obsolete]
    FeeChange = FinanceOperationType.FeeChange,

    [Display(Name = "Запрос льготы")]
    PreferentialFeeRequest = FinanceOperationType.PreferentialFeeRequest,

    [Display(Name = "Оплата", Description = "{0}: {1}")]
    Submit = FinanceOperationType.Submit,

    [Display(Name = "Оплата онлайн", Description = "{0}")]
    Online = FinanceOperationType.Online,

    [Display(Name = "Возврат", Description = "{0}")]
    Refund = FinanceOperationType.Refund,

    [Display(Name = "Исходящий перевод", Description = "{0} к ")]
    TransferTo = FinanceOperationType.TransferTo,

    [Display(Name = "Входящий перевод", Description = "{0} от ")]
    TransferFrom = FinanceOperationType.TransferFrom,

    [Display(Name = "Подписка", Description = "{0}")]
    OnlineSubscription = FinanceOperationType.OnlineSubscription,
}
