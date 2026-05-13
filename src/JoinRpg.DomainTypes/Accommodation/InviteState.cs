using System.ComponentModel;

namespace JoinRpg.DataModel;

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
