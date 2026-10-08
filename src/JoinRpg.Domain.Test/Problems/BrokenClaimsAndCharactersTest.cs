using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// Рассогласования между заявкой и персонажем.
/// </summary>
/// <remarks>
/// Третьей ветки прежнего правила — «у заявки нет персонажа» (<c>ClaimDontHaveTarget</c>) — здесь
/// нет и быть не может: в доменном агрегате заявка существует только как элемент
/// <c>CharacterInfo.Claims</c>, что проверяет конструктор <c>ClaimInCharacter</c>. Это
/// проверяется в <see cref="ClaimInfoTest"/>.
/// </remarks>
public class BrokenClaimsAndCharactersTest : ClaimProblemFilterTestBase
{
    private BrokenClaimsAndCharacters Filter { get; } = new BrokenClaimsAndCharacters();

    [Fact]
    public void ClaimInDiscussionOnOccupiedCharacterIsError()
    {
        var problems = Filter.GetProblems(MakeContext(
            claim => claim with { Status = ClaimStatus.Discussed },
            characterHasOtherApprovedClaim: true));

        problems.ShouldContain(p =>
            p.ProblemType == ClaimProblemType.ClaimActiveButCharacterHasApprovedClaim
            && p.Severity == ProblemSeverity.Error);
    }

    [Fact]
    public void ClaimInDiscussionOnVacantCharacterIsNotAProblem()
    {
        Filter.GetProblems(MakeContext(claim => claim with { Status = ClaimStatus.Discussed }))
            .ShouldBeEmpty();
    }

    /// <summary>
    /// Утверждённая заявка сама занимает персонажа, так что «персонаж уже занят» про неё не
    /// говорим — это правило только для обсуждаемых.
    /// </summary>
    [Fact]
    public void ApprovedClaimOnOccupiedCharacterIsNotReportedAsOccupied()
    {
        Filter.GetProblems(MakeContext(claim => claim with { Status = ClaimStatus.Approved }))
            .ShouldNotContain(p => p.ProblemType == ClaimProblemType.ClaimActiveButCharacterHasApprovedClaim);
    }

    [Fact]
    public void ApprovedClaimOnDeletedCharacterIsFatal()
    {
        var problems = Filter.GetProblems(MakeContext(
            claim => claim with { Status = ClaimStatus.Approved },
            characterIsActive: false));

        problems.ShouldContain(p =>
            p.ProblemType == ClaimProblemType.NoCharacterOnApprovedClaim
            && p.Severity == ProblemSeverity.Fatal);
    }

    [Fact]
    public void ApprovedClaimOnActiveCharacterIsNotAProblem()
    {
        Filter.GetProblems(MakeContext(claim => claim with { Status = ClaimStatus.Approved }))
            .ShouldBeEmpty();
    }

    /// <summary>Удалённый персонаж с неутверждённой заявкой — не повод жаловаться.</summary>
    [Fact]
    public void ClaimInDiscussionOnDeletedCharacterIsNotAProblem()
    {
        Filter.GetProblems(MakeContext(
            claim => claim with { Status = ClaimStatus.Discussed },
            characterIsActive: false))
            .ShouldBeEmpty();
    }
}
