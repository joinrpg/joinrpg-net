using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.UnifiedGrid;
using JoinRpg.DomainTypes.ProjectMetadata;
using static JoinRpg.DomainTypes.Test.Characters.CharacterInfoFixture;
using static JoinRpg.DomainTypes.Test.ProjectInfoFixture;

namespace JoinRpg.DomainTypes.Test.Characters;

/// <summary>
/// Правила статусов объединённого грида (ADR027 §2): какие персонажи попадают в грид и какие их
/// заявки показываются.
/// </summary>
public class UnifiedGridStatusTest
{
    private static readonly ProjectInfo Project = MakeProject();

    private static CharacterInfo Character(bool isActive = true, CharacterTypeInfo? type = null, CharacterClaimInfo[]? claims = null)
        => MakeCharacter(
            Project,
            characterTypeInfo: type,
            claims: claims ?? [],
            approvedClaimId: (claims ?? []).SingleOrDefault(claim => claim.IsApproved)?.ClaimId,
            isActive: isActive);

    private static CharacterClaimInfo Claim(int id, ClaimStatus status) => MakeClaim(Project, id, status);

    private static readonly CharacterTypeInfo Npc = new(CharacterType.NonPlayer, false, null, null, CharacterVisibility.Public);

    /// <summary>
    /// Удалённые персонажи нужны только архиву и «всем»; остальным их не надо даже загружать.
    /// </summary>
    [Theory]
    [InlineData(UnifiedGridStatus.Active, CharacterStatusSpec.Active)]
    [InlineData(UnifiedGridStatus.Vacant, CharacterStatusSpec.Active)]
    [InlineData(UnifiedGridStatus.Discussion, CharacterStatusSpec.Active)]
    [InlineData(UnifiedGridStatus.Approved, CharacterStatusSpec.Active)]
    [InlineData(UnifiedGridStatus.OnHold, CharacterStatusSpec.Active)]
    [InlineData(UnifiedGridStatus.Archive, CharacterStatusSpec.Any)]
    [InlineData(UnifiedGridStatus.All, CharacterStatusSpec.Any)]
    public void RepositorySpec(UnifiedGridStatus status, CharacterStatusSpec expected)
        => status.GetCharacterStatusSpec().ShouldBe(expected);

    [Fact]
    public void ActiveShowsLiveCharacterWithoutClaims()
        => UnifiedGridStatus.Active.Matches(Character()).ShouldBeTrue();

    [Fact]
    public void ActiveHidesDeletedCharacter()
        => UnifiedGridStatus.Active.Matches(Character(isActive: false)).ShouldBeFalse();

    [Fact]
    public void ActiveShowsOnlyActiveClaims()
    {
        var active = Claim(1, ClaimStatus.AddedByUser);
        var character = Character(claims: [active, Claim(2, ClaimStatus.DeclinedByUser), Claim(3, ClaimStatus.OnHold)]);

        UnifiedGridStatus.Active.SelectClaims(character).ShouldBe([active]);
    }

    [Fact]
    public void VacantShowsFreeCharacter()
        => UnifiedGridStatus.Vacant.Matches(Character()).ShouldBeTrue();

    [Fact]
    public void VacantHidesNpc()
        => UnifiedGridStatus.Vacant.Matches(Character(type: Npc)).ShouldBeFalse();

    [Fact]
    public void VacantHidesBusyCharacter()
        => UnifiedGridStatus.Vacant.Matches(Character(claims: [Claim(1, ClaimStatus.Approved)])).ShouldBeFalse();

    [Fact]
    public void DiscussionShowsCharacterWithActiveClaim()
        => UnifiedGridStatus.Discussion.Matches(Character(claims: [Claim(1, ClaimStatus.Discussed)])).ShouldBeTrue();

    [Fact]
    public void DiscussionHidesCharacterWithoutClaims()
        => UnifiedGridStatus.Discussion.Matches(Character()).ShouldBeFalse();

    [Fact]
    public void DiscussionHidesCharacterWithApprovedClaim()
        => UnifiedGridStatus.Discussion.Matches(
            Character(claims: [Claim(1, ClaimStatus.Approved), Claim(2, ClaimStatus.Discussed)]))
            .ShouldBeFalse();

    [Fact]
    public void ApprovedShowsCharacterWithApprovedClaim()
        => UnifiedGridStatus.Approved.Matches(
            Character(claims: [Claim(1, ClaimStatus.Approved), Claim(2, ClaimStatus.Discussed)]))
            .ShouldBeTrue();

    /// <summary>Принятая заявка одна — обсуждаемая соседка в «Принятых» не видна.</summary>
    [Fact]
    public void ApprovedShowsOnlyApprovedClaim()
    {
        var approved = Claim(1, ClaimStatus.Approved);
        var character = Character(claims: [approved, Claim(2, ClaimStatus.Discussed)]);

        UnifiedGridStatus.Approved.SelectClaims(character).ShouldBe([approved]);
    }

    [Fact]
    public void ApprovedHidesVacantCharacter()
        => UnifiedGridStatus.Approved.Matches(Character()).ShouldBeFalse();

    [Fact]
    public void ApprovedHidesDeletedCharacter()
        => UnifiedGridStatus.Approved.Matches(
            Character(isActive: false, claims: [Claim(1, ClaimStatus.Approved)]))
            .ShouldBeFalse();

    [Fact]
    public void OnHoldShowsCharacterWithOnHoldClaim()
        => UnifiedGridStatus.OnHold.Matches(
            Character(claims: [Claim(1, ClaimStatus.Approved), Claim(2, ClaimStatus.OnHold)]))
            .ShouldBeTrue();

    [Fact]
    public void OnHoldHidesCharacterWithoutOnHoldClaims()
        => UnifiedGridStatus.OnHold.Matches(
            Character(claims: [Claim(1, ClaimStatus.Approved), Claim(2, ClaimStatus.DeclinedByMaster)]))
            .ShouldBeFalse();

    [Fact]
    public void OnHoldHidesDeletedCharacter()
        => UnifiedGridStatus.OnHold.Matches(
            Character(isActive: false, claims: [Claim(1, ClaimStatus.OnHold)]))
            .ShouldBeFalse();

    /// <summary>В строке «Листа ожидания» только OnHold-заявки, принятая соседка не видна.</summary>
    [Fact]
    public void OnHoldShowsOnlyOnHoldClaims()
    {
        var onHold = Claim(2, ClaimStatus.OnHold);
        var character = Character(claims: [Claim(1, ClaimStatus.Approved), onHold]);

        UnifiedGridStatus.OnHold.SelectClaims(character).ShouldBe([onHold]);
    }

    [Fact]
    public void AllMatchesDeletedCharacterWithoutClaims()
        => UnifiedGridStatus.All.Matches(Character(isActive: false)).ShouldBeTrue();

    /// <summary>«Все» показывают все заявки — и у живых персонажей, и у удалённых.</summary>
    [Fact]
    public void AllShowsEveryClaimOfLiveAndDeletedCharacter()
    {
        var onHold = Claim(3, ClaimStatus.OnHold);
        var declined = Claim(2, ClaimStatus.DeclinedByMaster);
        var deleted = Character(isActive: false, claims: [onHold, declined]);
        var active = Claim(1, ClaimStatus.AddedByUser);
        var declinedByUser = Claim(4, ClaimStatus.DeclinedByUser);
        var live = Character(claims: [active, declinedByUser]);

        UnifiedGridStatus.All.SelectClaims(deleted).ShouldBe([onHold, declined], ignoreOrder: true);
        UnifiedGridStatus.All.SelectClaims(live).ShouldBe([active, declinedByUser], ignoreOrder: true);
    }

    [Fact]
    public void ArchiveShowsDeletedCharacterWithoutClaims()
        => UnifiedGridStatus.Archive.Matches(Character(isActive: false)).ShouldBeTrue();

    [Fact]
    public void ArchiveShowsAllClaimsOfDeletedCharacter()
    {
        var onHold = Claim(1, ClaimStatus.OnHold);
        var declined = Claim(2, ClaimStatus.DeclinedByMaster);
        var character = Character(isActive: false, claims: [onHold, declined]);

        UnifiedGridStatus.Archive.Matches(character).ShouldBeTrue();
        UnifiedGridStatus.Archive.SelectClaims(character).ShouldBe([onHold, declined], ignoreOrder: true);
    }

    [Theory]
    [InlineData(ClaimStatus.DeclinedByMaster)]
    [InlineData(ClaimStatus.DeclinedByUser)]
    public void ArchiveShowsDeclinedClaimsOfLiveCharacter(ClaimStatus declinedStatus)
    {
        var declined = Claim(2, declinedStatus);
        var character = Character(claims: [Claim(1, ClaimStatus.Approved), declined]);

        UnifiedGridStatus.Archive.Matches(character).ShouldBeTrue();
        UnifiedGridStatus.Archive.SelectClaims(character).ShouldBe([declined]);
    }

    /// <summary>
    /// Лист ожидания живого персонажа — не архив, хотя заявка там и неактивна.
    /// </summary>
    [Fact]
    public void ArchiveHidesOnHoldClaimOfLiveCharacter()
        => UnifiedGridStatus.Archive.Matches(
            Character(claims: [Claim(1, ClaimStatus.Approved), Claim(2, ClaimStatus.OnHold)]))
            .ShouldBeFalse();

    [Fact]
    public void ArchiveHidesLiveCharacterWithActiveClaimsOnly()
        => UnifiedGridStatus.Archive.Matches(Character(claims: [Claim(1, ClaimStatus.AddedByUser)])).ShouldBeFalse();
}
