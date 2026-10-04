using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Problems;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// Инварианты контекста расчёта проблем заявки.
/// </summary>
/// <remarks>
/// Контекст складывается из трёх объектов, загруженных по отдельности (персонаж, заявка, профиль
/// игрока). Если их не сверять, фильтрам можно передать заявку одного персонажа вместе с профилем
/// чужого игрока — и проблемы посчитались бы молча и неверно. Поэтому несогласованность
/// отвергается конструктором, а не «обрабатывается» ниже по течению.
/// </remarks>
public class ClaimProblemContextTest
{
    private MockedProject Mock { get; } = new MockedProject();

    [Fact]
    public void AcceptsClaimOfThisCharacterWithItsOwnPlayer()
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        var character = Mock.GetCharacterInfo(Mock.Character);

        var context = new ClaimProblemContext(character, character.GetClaimById(claim.GetId()), Mock.PlayerInfo);

        context.Claim.ClaimId.ShouldBe(claim.GetId());
        // ProjectInfo — производное от персонажа, второго канала метаданных у контекста нет.
        context.ProjectInfo.ShouldBeSameAs(character.ProjectInfo);
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
            () => new ClaimProblemContext(character, alienClaim, Mock.PlayerInfo));
        exception.Message.ShouldContain("is not among claims of character");
    }

    [Fact]
    public void RejectsProfileOfAnotherPlayer()
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        var character = Mock.GetCharacterInfo(Mock.Character);

        // MasterInfo — профиль другого пользователя, заявку подавал не он.
        var exception = Should.Throw<ArgumentException>(
            () => new ClaimProblemContext(character, character.GetClaimById(claim.GetId()), Mock.MasterInfo));
        exception.Message.ShouldContain("is passed for claim");
    }
}
