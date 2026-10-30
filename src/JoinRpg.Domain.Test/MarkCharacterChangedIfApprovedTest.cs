using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Правка утверждённой заявки — это правка персонажа: аудит персонажа должен сдвинуться.
/// </summary>
public class MarkCharacterChangedIfApprovedTest
{
    private static readonly DateTime Now = new(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);
    private const int UserId = 42;

    private readonly MockedProject mock = new();

    [Fact]
    public void ApprovedClaimMarksItsCharacter()
    {
        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);

        claim.MarkCharacterChangedIfApproved(mock.CreateCharacter("Другой"), Now, UserId);

        mock.Character.UpdatedAt.ShouldBe(Now);
        mock.Character.UpdatedById.ShouldBe(UserId);
    }

    [Theory]
    [InlineData(ClaimStatus.AddedByUser)]
    [InlineData(ClaimStatus.Discussed)]
    [InlineData(ClaimStatus.DeclinedByMaster)]
    public void NotApprovedClaimDoesNotMark(ClaimStatus status)
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.ClaimStatus = status;
        var before = (mock.Character.UpdatedAt, mock.Character.UpdatedById);

        claim.MarkCharacterChangedIfApproved(mock.Character, Now, UserId);

        (mock.Character.UpdatedAt, mock.Character.UpdatedById).ShouldBe(before);
    }

    [Fact]
    public void ClaimWithoutLoadedCharacterMarksFallback()
    {
        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);
        claim.Character = null!;

        claim.MarkCharacterChangedIfApproved(mock.Character, Now, UserId);

        mock.Character.UpdatedAt.ShouldBe(Now);
    }
}
