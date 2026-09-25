using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

public class ClaimPredicatesTest
{
    private static readonly UserIdentification Me = new(1);
    private static readonly UserIdentification SomebodyElse = new(2);

    // Главное меню показывает не проекты, а сами заявки игрока. Значит, выбирать надо ровно те
    // заявки, которые он реально может продолжать вести: активные, в неархивных проектах.

    private static Claim CreateClaim(
        UserIdentification player,
        ClaimStatus status = ClaimStatus.Approved,
        bool projectActive = true)
        => new()
        {
            PlayerUserId = player.Value,
            ClaimStatus = status,
            Project = new Project { Active = projectActive, Details = new ProjectDetails() },
        };

    private static bool IsMyActiveClaim(Claim claim)
        => ClaimPredicates.GetMyActiveClaimsInActiveProjects(Me).Compile()(claim);

    [Fact]
    public void MyApprovedClaimInActiveProject_ShouldBeSelected()
        => IsMyActiveClaim(CreateClaim(Me)).ShouldBeTrue();

    [Fact]
    public void MyDiscussedClaimInActiveProject_ShouldBeSelected()
        => IsMyActiveClaim(CreateClaim(Me, ClaimStatus.Discussed)).ShouldBeTrue();

    [Fact]
    public void ClaimOfAnotherPlayer_ShouldNotBeSelected()
        => IsMyActiveClaim(CreateClaim(SomebodyElse)).ShouldBeFalse();

    [Fact]
    public void DeclinedClaim_ShouldNotBeSelected()
        => IsMyActiveClaim(CreateClaim(Me, ClaimStatus.DeclinedByMaster)).ShouldBeFalse();

    [Fact]
    public void ClaimOnHold_ShouldNotBeSelected()
        => IsMyActiveClaim(CreateClaim(Me, ClaimStatus.OnHold)).ShouldBeFalse();

    [Fact]
    public void ClaimInArchivedProject_ShouldNotBeSelected()
        // Архивные игры доступны через «Архив игр», в меню их не показываем
        => IsMyActiveClaim(CreateClaim(Me, projectActive: false)).ShouldBeFalse();
}
