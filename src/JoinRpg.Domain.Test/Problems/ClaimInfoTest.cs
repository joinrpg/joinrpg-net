using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// Инварианты <see cref="ClaimInCharacter"/> и <see cref="ClaimInfo"/> (ADR021).
/// </summary>
/// <remarks>
/// Тип складывается из трёх объектов, загруженных по отдельности (персонаж, заявка, профиль
/// игрока). Если их не сверять, потребителю можно передать заявку одного персонажа вместе с профилем
/// чужого игрока — и, например, проблемы посчитались бы молча и неверно. Поэтому несогласованность
/// отвергается конструктором, а не «обрабатывается» ниже по течению.
/// Тесты лежат здесь, а не в <c>JoinRpg.DomainTypes.Test</c>, ради <see cref="MockedProject"/>.
/// </remarks>
public class ClaimInfoTest
{
    private MockedProject Mock { get; } = new MockedProject();

    [Fact]
    public void AcceptsClaimOfThisCharacterWithItsOwnPlayer()
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        var character = Mock.GetCharacterInfo(Mock.Character);

        var claimInfo = new ClaimInfo(new ClaimInCharacter(character, claim.GetId()), Mock.PlayerInfo);

        claimInfo.Claim.ClaimId.ShouldBe(claim.GetId());
        claimInfo.Character.ShouldBeSameAs(character);
        // ProjectInfo — производное от персонажа, второго канала метаданных у типа нет.
        claimInfo.ProjectInfo.ShouldBeSameAs(character.ProjectInfo);
    }

    [Fact]
    public void RejectsClaimOfAnotherCharacter()
    {
        var otherCharacter = Mock.CreateCharacter("Другой");
        var otherClaim = Mock.CreateClaim(otherCharacter, Mock.Player);
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);

        var character = Mock.GetCharacterInfo(Mock.Character);
        var alienClaim = Mock.GetCharacterInfo(otherCharacter).GetClaimById(otherClaim.GetId());

        var exception = Should.Throw<ArgumentException>(
            () => new ClaimInCharacter(character, alienClaim));
        exception.Message.ShouldContain("is not among claims of character");
    }

    [Fact]
    public void RejectsClaimIdOfAnotherCharacter()
    {
        var otherCharacter = Mock.CreateCharacter("Другой");
        var otherClaim = Mock.CreateClaim(otherCharacter, Mock.Player);
        var character = Mock.GetCharacterInfo(Mock.Character);

        _ = Should.Throw<KeyNotFoundException>(
            () => new ClaimInCharacter(character, otherClaim.GetId()));
    }

    [Fact]
    public void RejectsProfileOfAnotherPlayer()
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        var character = Mock.GetCharacterInfo(Mock.Character);

        // MasterInfo — профиль другого пользователя, заявку подавал не он.
        var exception = Should.Throw<ArgumentException>(
            () => new ClaimInfo(new ClaimInCharacter(character, claim.GetId()), Mock.MasterInfo));
        exception.Message.ShouldContain("is passed for claim");
    }
}
