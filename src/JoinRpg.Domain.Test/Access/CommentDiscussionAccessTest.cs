using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Comments;
using JoinRpg.DomainTypes.Forums;

namespace JoinRpg.Domain.Test.Access;

/// <summary>
/// Доступ игрока к обсуждению по владельцу (ADR026 §1) — замена цепочки
/// <c>HasPlayerAccess(CommentDiscussion)</c> через <c>Project.Claims</c> / <c>Project.ForumThreads</c>.
/// </summary>
public class CommentDiscussionAccessTest
{
    private readonly MockedProject mock = new();

    private UserIdentification PlayerId => new(mock.Player.UserId);
    private UserIdentification OtherUserId => new(mock.Master.UserId);

    private ForumDiscussionOwner ThreadOf(DataModel.CharacterGroup group, bool isVisibleToPlayer = true)
        => new(
            new ForumThreadIdentification(mock.ProjectInfo.ProjectId, 50),
            new CharacterGroupIdentification(mock.ProjectInfo.ProjectId, group.CharacterGroupId),
            isVisibleToPlayer);

    private ClaimInCharacter ClaimInCharacter(DataModel.Claim claim)
        => new(mock.GetCharacterInfo(claim.Character), claim.GetId());

    [Fact]
    public void Claim_OwnPlayer_HasAccess()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        new ClaimDiscussionOwner(claim.GetId()).HasPlayerAccess(ClaimInCharacter(claim), PlayerId).ShouldBeTrue();
    }

    [Fact]
    public void Claim_OtherUser_NoAccess()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        new ClaimDiscussionOwner(claim.GetId()).HasPlayerAccess(ClaimInCharacter(claim), OtherUserId).ShouldBeFalse();
    }

    [Fact]
    public void Claim_DeclinedClaimOfPlayer_HasAccess()
    {
        // Как и прежде (HasPlayerAccesToClaim), обсуждение своей заявки игрок видит в любом статусе.
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.ClaimStatus = ClaimStatus.DeclinedByMaster;

        new ClaimDiscussionOwner(claim.GetId()).HasPlayerAccess(ClaimInCharacter(claim), PlayerId).ShouldBeTrue();
    }

    [Fact]
    public void Claim_ClaimOfOtherDiscussion_Throws()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var other = new ClaimDiscussionOwner(new ClaimIdentification(mock.ProjectInfo.ProjectId, claim.ClaimId + 1));

        _ = Should.Throw<ArgumentException>(() => other.HasPlayerAccess(ClaimInCharacter(claim), PlayerId));
    }

    [Fact]
    public void Forum_ApprovedPlayerInGroup_HasAccess()
    {
        MockedProject.AddCharToGroup(mock.Character, mock.Group);
        _ = mock.CreateApprovedClaim(mock.Character, mock.Player);

        ThreadOf(mock.Group).HasPlayerAccess([mock.GetCharacterInfo(mock.Character)], PlayerId).ShouldBeTrue();
    }

    [Fact]
    public void Forum_CheckedInPlayerInGroup_HasAccess()
    {
        // Зарегистрированный на игре — тоже принятый: OfUserApproved / ClaimStatusSpec.Approved включают CheckedIn.
        MockedProject.AddCharToGroup(mock.Character, mock.Group);
        _ = mock.CreateCheckedInClaim(mock.Character, mock.Player);

        ThreadOf(mock.Group).HasPlayerAccess([mock.GetCharacterInfo(mock.Character)], PlayerId).ShouldBeTrue();
    }

    [Fact]
    public void Forum_ApprovedPlayerInChildGroup_HasAccess()
    {
        var child = mock.CreateCharacterGroup();
        child.ParentCharacterGroupIds = [mock.Group.CharacterGroupId];
        mock.ReInitProjectInfo();
        MockedProject.AddCharToGroup(mock.Character, child);
        _ = mock.CreateApprovedClaim(mock.Character, mock.Player);

        ThreadOf(mock.Group).HasPlayerAccess([mock.GetCharacterInfo(mock.Character)], PlayerId).ShouldBeTrue();
    }

    [Fact]
    public void Forum_ThreadHiddenFromPlayers_NoAccess()
    {
        MockedProject.AddCharToGroup(mock.Character, mock.Group);
        _ = mock.CreateApprovedClaim(mock.Character, mock.Player);

        ThreadOf(mock.Group, isVisibleToPlayer: false)
            .HasPlayerAccess([mock.GetCharacterInfo(mock.Character)], PlayerId)
            .ShouldBeFalse();
    }

    [Fact]
    public void Forum_CharacterNotInGroup_NoAccess()
    {
        _ = mock.CreateApprovedClaim(mock.Character, mock.Player);

        ThreadOf(mock.Group).HasPlayerAccess([mock.GetCharacterInfo(mock.Character)], PlayerId).ShouldBeFalse();
    }

    [Fact]
    public void Forum_ClaimNotApproved_NoAccess()
    {
        MockedProject.AddCharToGroup(mock.Character, mock.Group);
        _ = mock.CreateClaim(mock.Character, mock.Player);

        ThreadOf(mock.Group).HasPlayerAccess([mock.GetCharacterInfo(mock.Character)], PlayerId).ShouldBeFalse();
    }

    [Fact]
    public void Forum_ApprovedClaimOfOtherPlayer_NoAccess()
    {
        MockedProject.AddCharToGroup(mock.Character, mock.Group);
        _ = mock.CreateApprovedClaim(mock.Character, mock.Player);

        ThreadOf(mock.Group).HasPlayerAccess([mock.GetCharacterInfo(mock.Character)], OtherUserId).ShouldBeFalse();
    }
}
