using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain.Test.AddClaim;

/// <summary>
/// Правила заявки поверх доменного агрегата (ADR013) должны давать то же самое, что поверх
/// EF-сущности <see cref="Character"/>. Пока обе дороги живы, это единственное место, где
/// расхождение будет видно.
/// </summary>
public class ClaimValidationOverCharacterInfoTest
{
    private MockedProject Mock { get; } = new MockedProject();

    /// <summary>
    /// Считает причины двумя путями и требует совпадения, заодно возвращая результат — чтобы
    /// тест мог проверить и сам набор причин, а не только то, что реализации согласны.
    /// </summary>
    private IReadOnlyCollection<AddClaimForbideReason> BothWays(
        Character character,
        UserInfo? userInfo = null,
        ProjectInfo? projectInfo = null,
        ClaimOperation operation = ClaimOperation.AddByPlayer)
    {
        projectInfo ??= Mock.ProjectInfo;

        var overEntity = character.ValidateIfCanAddClaim(userInfo, projectInfo, operation).Kinds();
        var overAggregate = ClaimValidator
            .Validate(Mock.GetCharacterInfo(character), userInfo, movedClaim: null, projectInfo, operation)
            .Kinds();

        overAggregate.ShouldBe(overEntity, ignoreOrder: true);
        return overEntity;
    }

    [Fact]
    public void OrdinaryCharacterAllowed() => BothWays(Mock.Character, Mock.PlayerInfo).ShouldBeEmpty();

    [Fact]
    public void WithoutUserAllowed() => BothWays(Mock.Character).ShouldBeEmpty();

    [Fact]
    public void BusyCharacter()
    {
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Master);

        BothWays(Mock.Character, Mock.PlayerInfo).ShouldContain(AddClaimForbideReason.Busy);
    }

    [Fact]
    public void InactiveCharacter()
    {
        var inactive = Mock.CreateCharacter("inactive");
        inactive.IsActive = false;

        BothWays(inactive, Mock.PlayerInfo).ShouldContain(AddClaimForbideReason.CharacterInactive);
    }

    [Fact]
    public void Npc()
    {
        var npc = Mock.CreateCharacter("npc");
        npc.CharacterType = CharacterType.NonPlayer;

        BothWays(npc, Mock.PlayerInfo).ShouldContain(AddClaimForbideReason.Npc);
    }

    [Fact]
    public void ExhaustedSlot()
    {
        var slot = Mock.CreateCharacter("slot");
        slot.CharacterType = CharacterType.Slot;
        slot.CharacterSlotLimit = 0;

        BothWays(slot, Mock.PlayerInfo).ShouldContain(AddClaimForbideReason.SlotsExhausted);
    }

    [Fact]
    public void SlotWithPlaces()
    {
        var slot = Mock.CreateCharacter("slot");
        slot.CharacterType = CharacterType.Slot;
        slot.CharacterSlotLimit = 2;

        BothWays(slot, Mock.PlayerInfo).ShouldBeEmpty();
    }

    /// <summary>
    /// Единственное правило, где агрегат и EF-сущность читают заявки персонажа: у EF это
    /// <c>character.Claims</c>, у агрегата — <c>CharacterInfo.Claims</c>.
    /// </summary>
    [Fact]
    public void AlreadySent()
    {
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);

        BothWays(Mock.Character, Mock.PlayerInfo).ShouldContain(AddClaimForbideReason.AlreadySent);
    }

    [Fact]
    public void DeclinedClaimIsNotAlreadySent()
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        claim.ClaimStatus = ClaimStatus.DeclinedByMaster;

        BothWays(Mock.Character, Mock.PlayerInfo).ShouldNotContain(AddClaimForbideReason.AlreadySent);
    }

    [Fact]
    public void ClaimOfAnotherPlayerIsNotAlreadySent()
    {
        _ = Mock.CreateClaim(Mock.Character, Mock.Master);

        BothWays(Mock.Character, Mock.PlayerInfo).ShouldNotContain(AddClaimForbideReason.AlreadySent);
    }

    [Fact]
    public void ClaimsClosed()
    {
        var claimsClosed = Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.ActiveClaimsClosed);

        BothWays(Mock.Character, Mock.PlayerInfo, claimsClosed)
            .ShouldBe([AddClaimForbideReason.ProjectClaimsClosed]);
    }

    [Fact]
    public void MasterBypassesClaimsClosed()
    {
        var claimsClosed = Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.ActiveClaimsClosed);

        BothWays(Mock.Character, Mock.PlayerInfo, claimsClosed, ClaimOperation.AddByMaster).ShouldBeEmpty();
    }

    [Fact]
    public void ArchivedProject()
    {
        var archived = Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.Archived);

        BothWays(Mock.Character, Mock.PlayerInfo, archived)
            .ShouldBe([AddClaimForbideReason.ProjectNotActive]);
    }

    [Fact]
    public void SeveralReasonsAtOnce()
    {
        var character = Mock.CreateCharacter("bad");
        character.CharacterType = CharacterType.NonPlayer;
        character.IsActive = false;

        BothWays(character, Mock.PlayerInfo).ShouldBe(
            [AddClaimForbideReason.CharacterInactive, AddClaimForbideReason.Npc],
            ignoreOrder: true);
    }
}
