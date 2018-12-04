using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JoinRpg.Web.Models.Money
{
    public enum FinanceOperationStateViewModel
    {
        [Display(Name = "Подтверждено")]
        Approved,
        [Display(Name = "Ожидает подтверждения")]
        Proposed,
        [Display(Name = "Отклонено")]
        Declined,
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
    }
}
