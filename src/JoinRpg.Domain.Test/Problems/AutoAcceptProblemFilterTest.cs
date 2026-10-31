using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// Проблема автоприёма: исчерпанный лимит шаблона виден как проблема заявки, пока автоприём
/// включён и действует на заявку (<see cref="ClaimAutoAccept.AppliesTo"/>).
/// </summary>
public class AutoAcceptProblemFilterTest : ClaimProblemFilterTestBase
{
    private AutoAcceptProblemFilter Filter { get; } = new();

    /// <summary>
    /// Контекст с заявкой на шаблон. Автоприём включается в настройках проекта мока — как это
    /// делает мастер, — метаданные после этого пересобираются.
    /// </summary>
    private ClaimInfo MakeSlotContext(
        int? slotLimit,
        bool autoAccept = true,
        Func<CharacterClaimInfo, CharacterClaimInfo>? setupClaim = null,
        ProjectInfo? projectInfo = null)
    {
        if (autoAccept)
        {
            Mock.Project.Details.AutoAcceptClaims = true;
            Mock.ReInitProjectInfo();
        }

        return MakeContext(setupClaim, projectInfo, claimTarget: Mock.CreateSlot("Шаблон", slotLimit));
    }

    [Fact]
    public void NoProblemWhenAutoAcceptDisabled()
        => Filter.GetProblems(MakeSlotContext(slotLimit: 0, autoAccept: false)).ShouldBeEmpty();

    [Fact]
    public void NoProblemWhenSlotHasSpace()
        => Filter.GetProblems(MakeSlotContext(slotLimit: 5)).ShouldBeEmpty();

    /// <summary>null — лимита нет, места есть всегда.</summary>
    [Fact]
    public void NoProblemOnUnlimitedSlot()
        => Filter.GetProblems(MakeSlotContext(slotLimit: null)).ShouldBeEmpty();

    /// <summary>Автоприём действует только на шаблоны (#5645): на обычном персонаже проблемы нет.</summary>
    [Fact]
    public void NoProblemOnRegularCharacter()
    {
        Mock.Project.Details.AutoAcceptClaims = true;
        Mock.ReInitProjectInfo();

        Filter.GetProblems(MakeContext()).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ClaimStatus.AddedByUser)]
    [InlineData(ClaimStatus.Discussed)]
    public void ExhaustedSlotIsWarningOnActiveClaim(ClaimStatus status)
    {
        var problems = Filter.GetProblems(MakeSlotContext(slotLimit: 0, setupClaim: claim => claim with { Status = status }));

        var problem = problems.ShouldHaveSingleItem();
        problem.ProblemType.ShouldBe(ClaimProblemType.AutoAcceptSlotExhausted);
        problem.Severity.ShouldBe(ProblemSeverity.Warning);
        problem.ShouldBeOfType<AutoAcceptRelatedProblem>();
    }

    /// <summary>
    /// Принятой заявке автоприём уже не нужен: проблема не должна висеть в списке вечно.
    /// </summary>
    [Fact]
    public void NoProblemOnApprovedClaim()
        => Filter.GetProblems(MakeSlotContext(
                slotLimit: 0,
                setupClaim: claim => claim with { Status = ClaimStatus.Approved }))
            .ShouldBeEmpty();

    /// <summary>
    /// Блокер «не выдан доступ к защищённым данным» этот фильтр не дублирует: его уже показывает
    /// проблема SensitiveDataNotAllowed (ClaimContactsMissingFilter).
    /// </summary>
    [Fact]
    public void SensitiveDataBlockerIsNotDuplicated()
    {
        Mock.Project.Details.AutoAcceptClaims = true;
        Mock.ReInitProjectInfo();
        var passportRequired = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequirePassport = MandatoryStatus.Required });

        Filter.GetProblems(MakeSlotContext(
                slotLimit: 5,
                setupClaim: claim => claim with { PlayerAllowedSensitiveData = false },
                projectInfo: passportRequired))
            .ShouldBeEmpty();
    }
}
