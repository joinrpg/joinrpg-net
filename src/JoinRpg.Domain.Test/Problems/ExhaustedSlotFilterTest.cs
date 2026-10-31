using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// Незакрытая заявка на шаблоне без свободных мест — проблема сама по себе, автоприём тут
/// ни при чём (<see cref="ExhaustedSlotFilter"/>). Замечна ли она игроку, решает игрокская
/// выборка проблем, а не фильтр.
/// </summary>
public class ExhaustedSlotFilterTest : ClaimProblemFilterTestBase
{
    private ExhaustedSlotFilter Filter { get; } = new();

    private ClaimInfo MakeSlotContext(int? slotLimit, ClaimStatus status = ClaimStatus.AddedByUser)
        => MakeContext(
            claim => claim with { Status = status },
            claimTarget: Mock.CreateSlot("Шаблон", slotLimit));

    [Fact]
    public void NoProblemWhenSlotHasSpace()
        => Filter.GetProblems(MakeSlotContext(slotLimit: 5)).ShouldBeEmpty();

    /// <summary>null — лимита нет, места есть всегда.</summary>
    [Fact]
    public void NoProblemOnUnlimitedSlot()
        => Filter.GetProblems(MakeSlotContext(slotLimit: null)).ShouldBeEmpty();

    /// <summary>Автоприём — только для заявок на шаблон (#5645): на обычном персонаже проблемы нет.</summary>
    [Fact]
    public void NoProblemOnRegularCharacter()
        => Filter.GetProblems(MakeContext()).ShouldBeEmpty();

    /// <summary>Отклонённой заявке места в шаблоне уже не нужны.</summary>
    [Theory]
    [InlineData(ClaimStatus.DeclinedByMaster)]
    [InlineData(ClaimStatus.DeclinedByUser)]
    public void NoProblemOnDeclinedClaim(ClaimStatus status)
        => Filter.GetProblems(MakeSlotContext(slotLimit: 0, status: status)).ShouldBeEmpty();

    /// <summary>
    /// Пока заявка не закрыта, ей нужно место: в обсуждении ли она, приглашение ли сверх лимита
    /// (#4743) или на удержании.
    /// </summary>
    [Theory]
    [InlineData(ClaimStatus.AddedByUser)]
    [InlineData(ClaimStatus.AddedByMaster)]
    [InlineData(ClaimStatus.Discussed)]
    [InlineData(ClaimStatus.OnHold)]
    public void ExhaustedSlotIsWarningOnPendingClaim(ClaimStatus status)
    {
        var problems = Filter.GetProblems(MakeSlotContext(slotLimit: 0, status: status));

        var problem = problems.ShouldHaveSingleItem();
        problem.ProblemType.ShouldBe(ClaimProblemType.SlotExhausted);
        problem.Severity.ShouldBe(ProblemSeverity.Warning);
        problem.IsAutoAcceptBlocker.ShouldBeTrue();
    }
}
