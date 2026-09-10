using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain.Test.AddClaim;

/// <summary>
/// Правило «в проекте допустима только одна утверждённая заявка» считается по
/// <see cref="UserInfo.ActiveClaims"/>, а не по заявкам всего проекта из EF-графа.
/// </summary>
public class OnlyOneCharacterRuleTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private UserInfo WithClaims(params UserClaimInfo[] claims)
        => Mock.PlayerInfo with { ActiveClaims = claims };

    private ClaimIdentification ClaimIn(ProjectIdentification projectId, int claimId)
        => new(projectId, claimId);

    [Fact]
    public void ApprovedClaimInSameProjectBlocksNewClaim()
    {
        var userInfo = WithClaims(
            new UserClaimInfo(ClaimIn(Mock.ProjectInfo.ProjectId, 100), ClaimStatus.Approved));

        Mock.CreateCharacter("another")
            .ValidateIfCanAddClaim(userInfo, Mock.ProjectInfo, ClaimOperation.AddByPlayer).Kinds()
            .ShouldContain(AddClaimForbideReason.OnlyOneCharacter);
    }

    [Fact]
    public void CheckedInClaimCountsAsApproved()
    {
        var userInfo = WithClaims(
            new UserClaimInfo(ClaimIn(Mock.ProjectInfo.ProjectId, 100), ClaimStatus.CheckedIn));

        Mock.CreateCharacter("another")
            .ValidateIfCanAddClaim(userInfo, Mock.ProjectInfo, ClaimOperation.AddByPlayer).Kinds()
            .ShouldContain(AddClaimForbideReason.OnlyOneCharacter);
    }

    [Fact]
    public void UnapprovedClaimDoesNotBlock()
    {
        var userInfo = WithClaims(
            new UserClaimInfo(ClaimIn(Mock.ProjectInfo.ProjectId, 100), ClaimStatus.AddedByUser));

        Mock.CreateCharacter("another")
            .ValidateIfCanAddClaim(userInfo, Mock.ProjectInfo, ClaimOperation.AddByPlayer).Kinds()
            .ShouldNotContain(AddClaimForbideReason.OnlyOneCharacter);
    }

    /// <summary>
    /// Правило про этот проект, а не про все сразу: утверждённая заявка на другой игре не мешает.
    /// </summary>
    [Fact]
    public void ApprovedClaimInAnotherProjectDoesNotBlock()
    {
        var anotherProject = new ProjectIdentification(Mock.ProjectInfo.ProjectId.Value + 1);
        var userInfo = WithClaims(new UserClaimInfo(ClaimIn(anotherProject, 100), ClaimStatus.Approved));

        Mock.CreateCharacter("another")
            .ValidateIfCanAddClaim(userInfo, Mock.ProjectInfo, ClaimOperation.AddByPlayer).Kinds()
            .ShouldNotContain(AddClaimForbideReason.OnlyOneCharacter);
    }

    /// <summary>
    /// При переносе сама переносимая заявка не считается: иначе мастер не смог бы перенести
    /// утверждённую заявку никуда.
    /// </summary>
    [Fact]
    public void MovedClaimItselfDoesNotBlockMove()
    {
        var claim = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        var userInfo = WithClaims(new UserClaimInfo(claim.GetId(), ClaimStatus.Approved));

        Mock.CreateCharacter("another")
            .ValidateIfCanMoveClaim(claim, userInfo, Mock.ProjectInfo).Kinds()
            .ShouldNotContain(AddClaimForbideReason.OnlyOneCharacter);
    }

    [Fact]
    public void AnotherApprovedClaimStillBlocksMove()
    {
        var claim = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        var userInfo = WithClaims(
            new UserClaimInfo(claim.GetId(), ClaimStatus.Approved),
            new UserClaimInfo(ClaimIn(Mock.ProjectInfo.ProjectId, 999), ClaimStatus.Approved));

        Mock.CreateCharacter("another")
            .ValidateIfCanMoveClaim(claim, userInfo, Mock.ProjectInfo).Kinds()
            .ShouldContain(AddClaimForbideReason.OnlyOneCharacter);
    }

    [Fact]
    public void SettingOffDisablesRule()
    {
        var projectInfo = Mock.ProjectInfo.WithAllowManyClaims(strictlyOneCharacter: false);
        var userInfo = WithClaims(
            new UserClaimInfo(ClaimIn(Mock.ProjectInfo.ProjectId, 100), ClaimStatus.Approved));

        Mock.CreateCharacter("another")
            .ValidateIfCanAddClaim(userInfo, projectInfo, ClaimOperation.AddByPlayer).Kinds()
            .ShouldNotContain(AddClaimForbideReason.OnlyOneCharacter);
    }
}
