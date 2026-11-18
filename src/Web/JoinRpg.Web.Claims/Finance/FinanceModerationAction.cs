using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Claims.Finance;

/// <summary>
/// Что мастер делает в диалоге модерации финансовой операции.
/// </summary>
public enum FinanceModerationAction
{
    [Display(Name = "Оставить комментарий")]
    Comment,

    [Display(Name = "Подтвердить")]
    Approve,

    [Display(Name = "Отклонить")]
    Decline,
}
