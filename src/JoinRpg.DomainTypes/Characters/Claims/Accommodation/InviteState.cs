using System.ComponentModel;

namespace JoinRpg.DomainTypes.Characters.Claims.Accommodation;

//TODO[Localize]
public enum InviteState
{
    [Description("Не отвечено")]
    Unanswered,
    [Description("Принято")]
    Accepted,
    [Description("Отклонено")]
    Declined,
    [Description("Отменено")]
    Canceled,
}
