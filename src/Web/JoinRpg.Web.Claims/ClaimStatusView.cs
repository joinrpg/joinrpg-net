using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Claims;

public enum ClaimStatusView
{
    [Display(Name = "Подана")]
    AddedByUser,

    [Display(Name = "Предложена")]
    AddedByMaster,

    [Display(Name = "Принята")]
    Approved,

    [Display(Name = "Отозвана")]
    DeclinedByUser,

    [Display(Name = "Отклонена")]
    DeclinedByMaster,

    [Display(Name = "Обсуждается")]
    Discussed,

    [Display(Name = "В листе ожидания")]
    OnHold,

    [Display(Name = "Игрок заехал")]
    CheckedIn,
}

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

public class ClaimFullStatusView(ClaimStatusView claimStatus, ClaimDenialStatusView? claimDenialStatus)
{
    [Display(Name = "Статус")]
    public ClaimStatusView ClaimStatus { get; } = claimStatus;

    [Display(Name = "Причина отказа")]
    public ClaimDenialStatusView? ClaimDenialStatus { get; } = claimDenialStatus;
}
