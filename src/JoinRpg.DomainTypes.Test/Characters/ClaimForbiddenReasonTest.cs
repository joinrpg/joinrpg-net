using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.DomainTypes.Test.Characters;

public class ClaimForbiddenReasonTest
{
    [Fact]
    public void EveryReasonIsDescribedInTable()
    {
        foreach (var kind in Enum.GetValues<AddClaimForbideReason>())
        {
            var reason = Should.NotThrow(() => ClaimForbiddenReason.For(kind));
            reason.Kind.ShouldBe(kind);
        }
    }

    // Список причин, которые мастер вправе обойти, заморожен намеренно: если в
    // AddClaimForbideReason появится новое значение, решение «может ли мастер его обойти»
    // должно приниматься осознанно, а не достаться по умолчанию.
    // См. https://github.com/joinrpg/joinrpg-net/issues/4743 про Npc/CharacterInactive/SlotsExhausted.
    [Fact]
    public void OnlyExpectedReasonsCanBeOverridenByMaster()
    {
        var overridable = Enum.GetValues<AddClaimForbideReason>()
            .Where(kind => ClaimForbiddenReason.For(kind).MasterCanOverride)
            .ToList();

        overridable.ShouldBe(
            [
                AddClaimForbideReason.ProjectClaimsClosed,
                AddClaimForbideReason.RealNameMissing,
                AddClaimForbideReason.PhoneMissing,
                AddClaimForbideReason.TelegramMissing,
                AddClaimForbideReason.VkontakteMissing,
                AddClaimForbideReason.PassportMissing,
                AddClaimForbideReason.RegistrationAddressMissing,
            ],
            ignoreOrder: true);
    }

    // Фатальность — это то, на чём держится «показать только главную причину»: пока проект
    // в архиве или не принимает заявки, остальные причины игроку не показываем.
    [Fact]
    public void OnlyProjectLevelReasonsAreFatal()
    {
        var fatal = Enum.GetValues<AddClaimForbideReason>()
            .Where(kind => ClaimForbiddenReason.For(kind).IsFatal)
            .ToList();

        fatal.ShouldBe(
            [
                AddClaimForbideReason.ProjectNotActive,
                AddClaimForbideReason.ProjectClaimsClosed,
            ],
            ignoreOrder: true);
    }

    [Fact]
    public void ForReturnsSameInstance() =>
        ClaimForbiddenReason.For(AddClaimForbideReason.Busy)
            .ShouldBeSameAs(ClaimForbiddenReason.For(AddClaimForbideReason.Busy));

    [Fact]
    public void SeverityIsNeverHint() =>
        Enum.GetValues<AddClaimForbideReason>()
            .ShouldAllBe(kind => ClaimForbiddenReason.For(kind).Severity != ProblemSeverity.Hint);
}
