using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Действует ли автоприём на заявку (<see cref="ClaimInfo.IsAutoAcceptCandidate"/>). Заблокирована
/// ли заявка-кандидат, знают её проблемы (<see cref="ClaimProblem.IsAutoAcceptBlocker"/>).
/// </summary>
public class ClaimInfoAutoAcceptCandidateTest
{
    private MockedProject Mock { get; } = new();

    private ClaimInfo MakeClaimInfo(
        bool autoAcceptEnabled,
        ClaimStatus claimStatus = ClaimStatus.AddedByUser,
        CharacterType characterType = CharacterType.Slot)
    {
        Mock.Project.Details.AutoAcceptClaims = autoAcceptEnabled;
        Mock.ReInitProjectInfo();

        var character = characterType == CharacterType.Slot
            ? Mock.CreateSlot("Шаблон", slotLimit: 5)
            : Mock.CreateCharacter("Персонаж");
        var claim = Mock.CreateClaim(character, Mock.Player);
        claim.ClaimStatus = claimStatus;

        var characterInfo = Mock.GetCharacterInfo(character, Mock.ProjectInfo);
        return new ClaimInfo(new ClaimInCharacter(characterInfo, claim.GetId()), Mock.PlayerInfo);
    }

    [Theory]
    [InlineData(ClaimStatus.AddedByUser)]
    [InlineData(ClaimStatus.Discussed)]
    public void ActiveClaimOnSlotIsCandidate(ClaimStatus status)
        => MakeClaimInfo(autoAcceptEnabled: true, claimStatus: status).IsAutoAcceptCandidate().ShouldBeTrue();

    [Fact]
    public void DisabledAutoAcceptIsNotCandidate()
        => MakeClaimInfo(autoAcceptEnabled: false).IsAutoAcceptCandidate().ShouldBeFalse();

    /// <summary>Автоприём — только для заявок на шаблон (#5645).</summary>
    [Fact]
    public void RegularCharacterIsNotCandidate()
        => MakeClaimInfo(autoAcceptEnabled: true, characterType: CharacterType.Player).IsAutoAcceptCandidate().ShouldBeFalse();

    /// <summary>
    /// Принятой, отклонённой и прочим заявкам вне обсуждения автоприём не нужен: принимать нечего.
    /// </summary>
    [Theory]
    [InlineData(ClaimStatus.AddedByMaster)]
    [InlineData(ClaimStatus.Approved)]
    [InlineData(ClaimStatus.DeclinedByMaster)]
    [InlineData(ClaimStatus.DeclinedByUser)]
    [InlineData(ClaimStatus.OnHold)]
    [InlineData(ClaimStatus.CheckedIn)]
    public void ClaimOutsideDiscussionIsNotCandidate(ClaimStatus status)
        => MakeClaimInfo(autoAcceptEnabled: true, claimStatus: status).IsAutoAcceptCandidate().ShouldBeFalse();
}
