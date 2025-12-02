using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.Services.Email.Test;
public class MassProjectEmailServiceTest
{
    private readonly ClaimWithPlayer claimWithPlayer = new()
    {
        CharacterId = new CharacterIdentification(1, 1),
        CharacterName = "CharacterName",
        ClaimId = new ClaimIdentification(1, 1),
        ExtraNicknames = "",
        Player = player,
        ResponsibleMasterUserId = master.UserId,
    };

    private readonly ClaimWithPlayer masterClaim = new()
    {
        CharacterId = new CharacterIdentification(1, 2),
        CharacterName = "CharacterName",
        ClaimId = new ClaimIdentification(1, 2),
        ExtraNicknames = "",
        Player = master,
        ResponsibleMasterUserId = master.UserId,
    };


    private static readonly UserInfoHeader player = new(new UserIdentification(1), new UserDisplayName("PlayerName", null));
    private static readonly UserInfoHeader master = new(new UserIdentification(2), new UserDisplayName("Master", null));

    [Fact]
    public void DoublePlayerRemove()
    {
        var r = MassProjectEmailService.CalculateRecepients(false, [claimWithPlayer, claimWithPlayer], new MockedProject().ProjectInfo);
        r.Count.ShouldBe(1);
    }

    [Fact]
    public void MasterAndPlayerNotDoubled()
    {
        var r = MassProjectEmailService.CalculateRecepients(true, [claimWithPlayer, masterClaim], new MockedProject().ProjectInfo);
        r.Count.ShouldBe(2);
    }

    [Fact]
    public void MasterAndPlayerCorrect()
    {
        var r = MassProjectEmailService.CalculateRecepients(true, [claimWithPlayer], new MockedProject().ProjectInfo);
        r.Count.ShouldBe(2);
    }
}
