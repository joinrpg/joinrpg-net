using System.Collections.Frozen;

namespace JoinRpg.DomainTypes.Characters.Claims;

/// <summary>
/// Причина, по которой заявку нельзя подать или перенести, вместе с её свойствами.
/// </summary>
/// <param name="Kind">
/// Собственно причина. Остаётся идентификатором: на неё завязаны тексты в UI.
/// </param>
/// <param name="MasterCanOverride">
/// Мастер может выполнить операцию несмотря на эту причину — например пригласить игрока в проект
/// с закрытым приёмом заявок. Сам факт обхода определяется операцией, а не этим флагом: см.
/// вызывающий код.
/// </param>
/// <param name="Severity">
/// Насколько причина безнадёжна. <see cref="ProblemSeverity.Fatal"/> означает «делать тут больше
/// нечего»: при наличии хотя бы одной фатальной причины остальные не показываются, потому что
/// сначала надо разобраться с ней.
/// </param>
public record class ClaimForbiddenReason(
    AddClaimForbideReason Kind,
    bool MasterCanOverride,
    ProblemSeverity Severity)
{
    public bool IsFatal => Severity == ProblemSeverity.Fatal;

    public static ClaimForbiddenReason For(AddClaimForbideReason kind) => Table[kind];

    private static readonly FrozenDictionary<AddClaimForbideReason, ClaimForbiddenReason> Table =
        Enum.GetValues<AddClaimForbideReason>().ToFrozenDictionary(kind => kind, Create);

    private static ClaimForbiddenReason Create(AddClaimForbideReason kind) => kind switch
    {
        // Проект в архиве — не поможет никто и ничто.
        AddClaimForbideReason.ProjectNotActive
            => new(kind, MasterCanOverride: false, ProblemSeverity.Fatal),

        // Приём заявок закрыт: игроку тут делать нечего, но мастер вправе позвать игрока сам.
        AddClaimForbideReason.ProjectClaimsClosed
            => new(kind, MasterCanOverride: true, ProblemSeverity.Fatal),

        // Свойства самого персонажа. Мастер может поменять их руками, но пока это осознанно
        // жёсткие запреты — см. https://github.com/joinrpg/joinrpg-net/issues/4743.
        AddClaimForbideReason.SlotsExhausted or AddClaimForbideReason.Npc
            or AddClaimForbideReason.Busy or AddClaimForbideReason.CharacterInactive
            => new(kind, MasterCanOverride: false, ProblemSeverity.Error),

        // Ограничения целостности: две заявки на одного персонажа или две утверждённые заявки
        // в проекте, который этого не допускает.
        AddClaimForbideReason.AlreadySent or AddClaimForbideReason.OnlyOneCharacter
            => new(kind, MasterCanOverride: false, ProblemSeverity.Error),

        // Ограничения переноса заявки.
        AddClaimForbideReason.ApprovedClaimMovedToSlot
            or AddClaimForbideReason.CheckedInClaimCantBeMoved
            => new(kind, MasterCanOverride: false, ProblemSeverity.Error),

        // Недозаполненный профиль игрока. Мастер может пригласить игрока и попросить дозаполнить
        // профиль потом.
        AddClaimForbideReason.RealNameMissing or AddClaimForbideReason.PhoneMissing
            or AddClaimForbideReason.TelegramMissing or AddClaimForbideReason.VkontakteMissing
            => new(kind, MasterCanOverride: true, ProblemSeverity.Warning),

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, message: null),
    };
}
