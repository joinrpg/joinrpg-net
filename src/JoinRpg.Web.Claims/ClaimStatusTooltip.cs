using JoinRpg.Helpers;

namespace JoinRpg.Web.Claims;

public static class ClaimStatusTooltip
{
    public static string Build(ClaimStatusView status, ClaimDenialStatusView? denialStatus, bool isMaster) =>
        status switch
        {
            ClaimStatusView.AddedByUser => isMaster
                ? "Игрок подал заявку, и она ждет решения мастера"
                : "Вы подали заявку, она ждет решения мастера",

            ClaimStatusView.AddedByMaster => isMaster
                ? "Вы предложили эту роль игроку. Дождитесь его ответа — он может принять или отклонить предложение."
                : "Мастер предложил вам эту роль. Примите или отклоните предложение.",

            ClaimStatusView.Approved => isMaster
                ? "Заявка принята, роль закреплена за игроком"
                : "Заявка принята, роль закреплена за вами",

            ClaimStatusView.DeclinedByUser => isMaster
                ? "Игрок отозвал заявку"
                : "Вы отозвали заявку",

            ClaimStatusView.DeclinedByMaster => BuildDeclinedByMasterText(denialStatus, isMaster),

            ClaimStatusView.Discussed => isMaster
                ? "Заявка обсуждается, примите или отклоните ее"
                : "Заявка обсуждается",

            ClaimStatusView.OnHold => "Заявка в листе ожидания",

            ClaimStatusView.CheckedIn => isMaster
                ? "Игрок отмечен как заехавший на игру"
                : "Вы отмечены как заехавший на игру",

            _ => string.Empty,
        };

    private static string BuildDeclinedByMasterText(ClaimDenialStatusView? denialStatus, bool isMaster)
    {
        var reason = denialStatus?.GetDisplayName();
        var reasonSuffix = string.IsNullOrEmpty(reason) ? "" : $" Причина: {reason}.";

        return isMaster
            ? $"Вы отклонили заявку игрока.{reasonSuffix} "
            : $"Мастера отклонили заявку.";
    }
}
