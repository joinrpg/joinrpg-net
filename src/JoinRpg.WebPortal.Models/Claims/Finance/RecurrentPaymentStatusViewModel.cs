using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel.Finances;

namespace JoinRpg.Web.Models;

public enum RecurrentPaymentStatusViewModel
{
    [Display(Name = "Ошибка")]
    Failed = RecurrentPaymentStatus.Failed,

    [Display(Name = "Создана")]
    Created = RecurrentPaymentStatus.Created,

    [Display(Name = "Ожидает подтверждения")]
    Initialization = RecurrentPaymentStatus.Initialization,

    [Display(Name = "Оформлена")]
    Active = RecurrentPaymentStatus.Active,

    [Display(Name = "Деактивирована")]
    Cancelling = RecurrentPaymentStatus.Cancelling,

    [Display(Name = "Отменена")]
    Cancelled = RecurrentPaymentStatus.Cancelled,

    [Display(Name = "Оформлена новая")]
    Superseded = RecurrentPaymentStatus.Superseded,
}
