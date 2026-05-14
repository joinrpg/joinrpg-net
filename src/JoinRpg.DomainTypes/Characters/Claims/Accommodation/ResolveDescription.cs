using System.ComponentModel;

namespace JoinRpg.DomainTypes.Claims.Accommodation;

//TODO[Localize]
public enum ResolveDescription
{
    [Description("Не указан")]
    Unspecified,
    [Description("Отправлено")]
    Open,
    [Description("Принято игроком")]
    Accepted,
    [Description("Принято автоматически")]
    AcceptedAuto,
    [Description("Принято мастером")]
    AcceptedByMaster,
    [Description("Отколнено игроком")]
    Declined,
    [Description("Отколнено автоматически")]
    DeclinedAuto,
    [Description("Отколнено мастером")]
    DeclinedByMaster,
    [Description("Отколнено принятием другого приглашения")]
    DeclinedWithAcceptOther,
    [Description("Отозвано")]
    Canceled,
    [Description("Связанная заявка отозвана")]
    ClaimCanceled,
}
