using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models.Money
{
    public enum FinanceOperationStateViewModel
    {
        [Display(Name = "Подтверждено", ShortName = "Проведено")]
        Approved,
        [Display(Name = "Ожидает подтверждения", ShortName = "Оплата")]
        Proposed,
        [Display(Name = "Отклонено", ShortName = "Отклонено")]
        Declined,
        [Display(Name = "Отменено", ShortName = "Отменено")]
        Invalid,
    }

    public enum MoneyTransferStateViewModel
    {
        [Display(Name = "Подтверждено")]
        Approved,
        [Display(Name = "Отклонено")]
        Declined,
        [Display(Name = "Должен подтвердить получатель")]
        PendingForReceiver,
        [Display(Name = "Должен подтвердить отправитель")]
        PendingForSender,
        [Display(Name = "Нужно подтвердить")]
        PendingForBoth,
    }
}
