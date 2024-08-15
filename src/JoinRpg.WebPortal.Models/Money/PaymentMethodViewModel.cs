using System.ComponentModel.DataAnnotations;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models;

public enum PaymentMethodViewModel
{
    [Display(Name = "Оплатить картой", Description = "Оплата банковской картой МИР — нужно будет ввести данные карты")]
    BankCard = PaymentMethod.BankCard,

    [Display(Name = "Оплатить по QR-коду", Description = "Оплата через Систему Быстрых Платежей при помощи QR-кода — нужно будет отсканировать QR-код в приложении вашего банка")]
    FastPaymentsSystem = PaymentMethod.FastPaymentsSystem,
}
