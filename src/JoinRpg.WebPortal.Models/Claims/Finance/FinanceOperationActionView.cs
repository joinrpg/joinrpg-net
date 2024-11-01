using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models;

public enum FinanceOperationActionView
{
    [Display(Name = "Ничего не делать")]
    None,
    [Display(Name = "Подтвердить операцию")]
    Approve,
    [Display(Name = "Отменить операцию")]
    Decline,
}

