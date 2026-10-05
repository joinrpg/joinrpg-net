using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.Access;

/// <summary>
/// Правило «кто может менять проживание» общее у операций и у UI (#5261): по нему страница заявки
/// решает, показывать ли кнопки, поэтому оно должно отказывать ровно там, где откажет операция.
/// </summary>
public class AccommodationAccessTest
{
    private readonly MockedProject mock = new();

    private static readonly UserIdentification Stranger = new(99);

    [Theory]
    [InlineData(ClaimStatus.Approved, true)]
    [InlineData(ClaimStatus.CheckedIn, false)]
    [InlineData(ClaimStatus.AddedByUser, false)]
    [InlineData(ClaimStatus.Discussed, false)]
    [InlineData(ClaimStatus.OnHold, false)]
    public void Player_CanChangeOnlyApprovedClaim(ClaimStatus status, bool expected)
    {
        var claim = CreateClaim(status);

        claim.CanChangeAccommodation(new UserIdentification(mock.Player.UserId)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(ClaimStatus.Approved, true)]
    [InlineData(ClaimStatus.CheckedIn, false)]
    [InlineData(ClaimStatus.Discussed, false)]
    public void ResponsibleMasterWithoutPermission_CanChangeOnlyApprovedClaim(ClaimStatus status, bool expected)
    {
        var claim = CreateClaim(status);
        var master = CreateMasterWithoutAccommodationPermission();
        claim.ResponsibleMasterUser = master;
        claim.ResponsibleMasterUserId = master.UserId;

        claim.CanChangeAccommodation(new UserIdentification(master.UserId)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(ClaimStatus.Approved)]
    [InlineData(ClaimStatus.CheckedIn)]
    [InlineData(ClaimStatus.Discussed)]
    public void MasterWithPermission_CanChangeAnyClaim(ClaimStatus status)
    {
        var claim = CreateClaim(status);
        var master = mock.CreateMaster();

        claim.CanChangeAccommodation(new UserIdentification(master.UserId)).ShouldBeTrue();
    }

    [Fact]
    public void MasterWithoutPermission_CannotChangeNotOwnClaim()
    {
        var claim = CreateClaim(ClaimStatus.Approved);
        var master = CreateMasterWithoutAccommodationPermission();

        claim.CanChangeAccommodation(new UserIdentification(master.UserId)).ShouldBeFalse();
    }

    [Fact]
    public void Stranger_CannotChange()
        => CreateClaim(ClaimStatus.Approved).CanChangeAccommodation(Stranger).ShouldBeFalse();

    [Fact]
    public void Anonymous_CannotChange()
        => CreateClaim(ClaimStatus.Approved).CanChangeAccommodation(null).ShouldBeFalse();

    [Fact]
    public void RequestAccess_WhenDenied_Throws()
    {
        var claim = CreateClaim(ClaimStatus.CheckedIn);

        _ = Should.Throw<NoAccessToProjectException>(
            () => claim.RequestAccommodationChangeAccess(new UserIdentification(mock.Player.UserId)));
    }

    private Claim CreateClaim(ClaimStatus status)
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.ClaimStatus = status;
        return claim;
    }

    private User CreateMasterWithoutAccommodationPermission()
    {
        var master = mock.CreateMaster();
        mock.Project.ProjectAcls.Single(acl => acl.UserId == master.UserId).CanSetPlayersAccommodations = false;
        return master;
    }
}
