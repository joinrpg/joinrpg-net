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

        CanChange(claim, new UserIdentification(mock.Player.UserId)).ShouldBe(expected);
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

        CanChange(claim, new UserIdentification(master.UserId)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(ClaimStatus.Approved)]
    [InlineData(ClaimStatus.CheckedIn)]
    [InlineData(ClaimStatus.Discussed)]
    public void MasterWithPermission_CanChangeAnyClaim(ClaimStatus status)
    {
        var claim = CreateClaim(status);
        var master = mock.CreateMaster();

        CanChange(claim, new UserIdentification(master.UserId)).ShouldBeTrue();
    }

    [Fact]
    public void MasterWithoutPermission_CannotChangeNotOwnClaim()
    {
        var claim = CreateClaim(ClaimStatus.Approved);
        var master = CreateMasterWithoutAccommodationPermission();

        CanChange(claim, new UserIdentification(master.UserId)).ShouldBeFalse();
    }

    [Fact]
    public void Stranger_CannotChange()
        => CanChange(CreateClaim(ClaimStatus.Approved), Stranger).ShouldBeFalse();

    [Fact]
    public void Anonymous_CannotChange()
        => CanChange(CreateClaim(ClaimStatus.Approved), null).ShouldBeFalse();

    [Fact]
    public void RequestAccess_WhenDenied_Throws()
    {
        var claim = CreateClaim(ClaimStatus.CheckedIn);
        var player = new UserIdentification(mock.Player.UserId);

        _ = Should.Throw<NoAccessToProjectException>(() => claim.RequestAccommodationChangeAccess(player));
        _ = Should.Throw<NoAccessToProjectException>(() => Snapshot(claim).RequestAccommodationChangeAccess(player));
    }

    private Claim CreateClaim(ClaimStatus status)
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.ClaimStatus = status;
        // Иначе снимок персонажа справедливо отвергнет утверждённую заявку, не отмеченную у персонажа.
        mock.Character.ApprovedClaimId = status is ClaimStatus.Approved or ClaimStatus.CheckedIn ? claim.ClaimId : null;
        return claim;
    }

    /// <summary>Снимок той же заявки — метаданные перечитываются, чтобы увидеть новых мастеров.</summary>
    private ClaimInCharacter Snapshot(Claim claim)
    {
        mock.ReInitProjectInfo();
        return new ClaimInCharacter(mock.GetCharacterInfo(mock.Character), claim.GetId());
    }

    /// <summary>
    /// Решение по снимку (ADR022) — и сверка с перегрузкой над EF-заявкой: правило одно, и
    /// кнопки на странице заявки обязаны совпадать с тем, что разрешит операция.
    /// </summary>
    private bool CanChange(Claim claim, UserIdentification? user)
    {
        var viaSnapshot = Snapshot(claim).CanChangeAccommodation(user);
        claim.CanChangeAccommodation(user).ShouldBe(viaSnapshot);
        return viaSnapshot;
    }

    private User CreateMasterWithoutAccommodationPermission()
    {
        var master = mock.CreateMaster();
        mock.Project.ProjectAcls.Single(acl => acl.UserId == master.UserId).CanSetPlayersAccommodations = false;
        return master;
    }
}
