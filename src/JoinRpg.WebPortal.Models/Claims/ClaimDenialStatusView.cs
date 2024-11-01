using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models;

public enum ClaimDenialStatusView
{
    [Display(Name = "Мастера не могут предоставить желаемую роль")]
    Unavailable,
    [Display(Name = "Игрок сам отказался, но не отозвал заявку")]
    Refused,
    [Display(Name = "Игрок не отвечает")]
    NotRespond,
    [Display(Name = "Мастера снимают игрока с роли")]
    Removed,
    [Display(Name = "Мастера игроку на игре не рады")]
    NotSuitable,
    [Display(Name = "Игрок не выполнил условия участия или не сдал взнос")]
    NotImplementable,
}
