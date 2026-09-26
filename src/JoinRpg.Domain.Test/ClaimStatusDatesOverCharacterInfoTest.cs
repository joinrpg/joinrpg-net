using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Снимок заявки (<see cref="CharacterClaimInfo"/>) должен нести все статусные даты заявки, а не
/// только дату регистрации. Пока даты не были частью снимка, правило поверх агрегата не могло
/// отличить «мастер отклонил» от «игрок отозвал» — тест закрепляет, что теперь может.
/// </summary>
public class ClaimStatusDatesOverCharacterInfoTest
{
    private readonly MockedProject mock = new();

    private static readonly DateTime MasterAccepted = new(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MasterDeclined = new(2024, 5, 2, 11, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PlayerDeclined = new(2024, 5, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CheckedIn = new(2024, 5, 4, 13, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void StatusDatesSetOnEntityShouldBeVisibleOnSnapshot()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.MasterAcceptedDate = MasterAccepted;
        claim.MasterDeclinedDate = MasterDeclined;
        claim.PlayerDeclinedDate = PlayerDeclined;
        claim.CheckInDate = CheckedIn;

        var claimInfo = mock.GetCharacterInfo(mock.Character).Claims.Single();

        claimInfo.MasterAcceptedDate.ShouldBe(MasterAccepted);
        claimInfo.MasterDeclinedDate.ShouldBe(MasterDeclined);
        claimInfo.PlayerDeclinedDate.ShouldBe(PlayerDeclined);
        claimInfo.CheckInDate.ShouldBe(CheckedIn);
    }

    [Fact]
    public void UnsetStatusDatesShouldStayNullOnSnapshot()
    {
        mock.CreateClaim(mock.Character, mock.Player);

        var claimInfo = mock.GetCharacterInfo(mock.Character).Claims.Single();

        claimInfo.MasterAcceptedDate.ShouldBeNull();
        claimInfo.MasterDeclinedDate.ShouldBeNull();
        claimInfo.PlayerDeclinedDate.ShouldBeNull();
        claimInfo.CheckInDate.ShouldBeNull();
    }
}
