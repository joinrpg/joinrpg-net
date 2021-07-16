using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace JoinRpg.Web.Models
{
    public enum FinanceOperationActionView
    {
        [Display(Name = "Ничего не делать"), UsedImplicitly]
        None,
        [Display(Name = "Подтвердить операцию"), UsedImplicitly]
        Approve,
        [Display(Name = "Отменить операцию"), UsedImplicitly]
        Decline,
    }
}

