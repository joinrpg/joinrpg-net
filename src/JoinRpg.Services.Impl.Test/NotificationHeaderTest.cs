using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Services.Email;
using JoinRpg.Services.Impl;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Impl.Test.Projects;

namespace JoinRpg.Services.Impl.Test;

public class NotificationHeaderTest
{
    private readonly MockedProject mock = new();
    private readonly FakeNotificationService notifications = new();
    private readonly FakeProjectMetadataRepository metadataRepository;

    public NotificationHeaderTest()
    {
        metadataRepository = new FakeProjectMetadataRepository(mock);
    }

    [Fact]
    public async Task PlotEmail_HeaderContainsOnlyProjectNameValue()
    {
        var projectId = mock.ProjectInfo.ProjectId;
        var claimId = new ClaimIdentification(projectId, 1);
        var plotElementId = new PlotElementIdentification(
            new PlotFolderIdentification(projectId, 1),
            PlotElementId: 1);

        var claimsRepository = new FakeClaimsRepository(
            new ClaimWithPlayer
            {
                ClaimId = claimId,
                CharacterId = new CharacterIdentification(projectId, 1),
                CharacterName = "Character",
                ExtraNicknames = "",
                Player = new UserInfoHeader(new UserIdentification(1), new UserDisplayName("Player", null)),
                ResponsibleMasterUserId = new UserIdentification(2),
            });

        var service = new MassProjectEmailService(
            claimsRepository,
            metadataRepository,
            notifications,
            new FakeCurrentUserAccessor(mock.Master.UserId),
            new FakeUriLocator());

        await service.PlotEmail([claimId], new MarkdownDbValue("тело вводной"), plotElementId);

        var notification = notifications.Queued.ShouldHaveSingleItem();
        notification.Header.ShouldBe("Mocked project: опубликована вводная");
        notification.Header.ShouldNotContain("ProjectName(");
    }

    [Fact]
    public async Task ForumNotification_HeaderContainsOnlyProjectNameValue()
    {
        var projectId = mock.ProjectInfo.ProjectId;
        var threadId = new ForumThreadIdentification(projectId, ThreadId: 1);
        var commentId = new ForumCommentIdentification(threadId, CommentId: 1);

        var claimsRepository = new FakeClaimsRepository();
        var forumRepository = new FakeForumRepository(
            new ForumThreadHeader("Test thread", new CharacterGroupIdentification(projectId, mock.Group.CharacterGroupId)));

        var subscribeCalculator = new SubscribeCalculator(
            new FakeUserSubscribeRepository(),
            new FakeCharacterRepository(),
            claimsRepository);

        var service = new ForumNotificationService(
            subscribeCalculator,
            notifications,
            metadataRepository,
            forumRepository);

        await service.SendNotification(new ForumMessageNotification(
            commentId,
            new UserInfoHeader(new UserIdentification(mock.Master.UserId), new UserDisplayName("Master", null)),
            new MarkdownDbValue("текст сообщения"),
            Header: "заголовок из параметра игнорируется"));

        var notification = notifications.Queued.ShouldHaveSingleItem();
        notification.Header.ShouldBe("Mocked project: тема на форуме Test thread");
        notification.Header.ShouldNotContain("ProjectName(");
    }

    private sealed class FakeClaimsRepository(params ClaimWithPlayer[] claims) : IClaimsRepository
    {
        public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(IReadOnlyCollection<ClaimIdentification> claimIds)
            => Task.FromResult<IReadOnlyCollection<ClaimWithPlayer>>(claims);

        public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(IReadOnlyCollection<CharacterGroupIdentification> characterGroupsIds, ClaimStatusSpec spec)
            => Task.FromResult<IReadOnlyCollection<ClaimWithPlayer>>([]);

        public Task<IReadOnlyCollection<Claim>> GetClaims(int projectId, ClaimStatusSpec status) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(UserIdentification userId, ClaimStatusSpec status) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(ProjectIdentification projectId, UserIdentification userId, ClaimStatusSpec status) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForMaster(int projectId, int userId, ClaimStatusSpec status) => throw new NotSupportedException();
        public Task<Claim?> GetClaim(ClaimIdentification claimId) => throw new NotSupportedException();
        public Task<Claim?> GetClaimWithDetails(ClaimIdentification claimId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForGroups(ProjectIdentification projectId, ClaimStatusSpec active, CharacterGroupIdentification[] characterGroupsIds) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimsHeadersForPlayer(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec, UserIdentification userId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ClaimCountByMaster>> GetClaimsCountByMasters(int projectId, ClaimStatusSpec claimStatusSpec) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(ProjectIdentification projectId, ClaimStatusSpec approved) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForRoomType(int projectId, ClaimStatusSpec claimStatusSpec, int? roomTypeId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForMoneyTransfersListAsync(int projectId, ClaimStatusSpec claimStatusSpec) => throw new NotSupportedException();
        public Task<Dictionary<int, int>> GetUnreadDiscussionsForClaims(int projectId, ClaimStatusSpec claimStatusSpec, int userId, bool hasMasterAccess) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class FakeUriLocator : IUriLocator<ClaimIdentification>
    {
        public Uri GetUri(ClaimIdentification target) => new("https://example.com/claim");
    }

    private sealed class FakeForumRepository(ForumThreadHeader threadHeader) : IForumRepository
    {
        public Task<ForumThread> GetThread(ForumThreadIdentification forumThreadId) => throw new NotSupportedException();
        public Task<CommentDiscussion> GetDiscussion(int projectId, int commentDiscussionId) => throw new NotSupportedException();
        public Task<CommentDiscussion> GetDiscussionByComment(int projectId, int commentId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<IForumThreadListItem>> GetThreads(int projectId, bool isMaster, IEnumerable<int>? groupIds) => throw new NotSupportedException();
        public Task<ForumThreadHeader> GetThreadHeader(ForumThreadIdentification threadId) => Task.FromResult(threadHeader);
    }

    private sealed class FakeUserSubscribeRepository : IUserSubscribeRepository
    {
        public Task<IReadOnlyCollection<UserSubscribe>> GetDirect(IReadOnlyCollection<ClaimIdentification> claimId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserSubscribe>> GetForCharAndGroups(IReadOnlyCollection<CharacterGroupIdentification> characterGroupIdentifications, IReadOnlyCollection<CharacterIdentification> characterId) => throw new NotSupportedException();
        public Task<(User User, UserSubscriptionDto[] UserSubscriptions)> LoadSubscriptionsForProject(UserIdentification userId, ProjectIdentification projectId) => throw new NotSupportedException();
        public Task<UserSubscriptionDto> LoadSubscriptionById(ProjectIdentification projectId, int subscriptionId) => throw new NotSupportedException();
    }

    private sealed class FakeCharacterRepository : ICharacterRepository
    {
        public Task<IReadOnlyCollection<CharacterHeader>> GetCharacterHeaders(int projectId, DateTime? modifiedSince) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Character>> GetCharacters(IReadOnlyCollection<CharacterIdentification> characterIds) => Task.FromResult<IReadOnlyCollection<Character>>([]);
        public Task<Character> GetCharacterAsync(int projectId, int characterId) => throw new NotSupportedException();
        public Task<Character> GetCharacterAsync(CharacterIdentification characterId) => throw new NotSupportedException();
        public Task<Character> GetCharacterWithGroups(int projectId, int characterId) => throw new NotSupportedException();
        public Task<Character> GetCharacterWithDetails(int projectId, int characterId) => throw new NotSupportedException();
        public Task<CharacterView> GetCharacterViewAsync(int projectId, int characterId) => throw new NotSupportedException();
        public Task<IEnumerable<Character>> GetAvailableCharacters(ProjectIdentification projectId) => throw new NotSupportedException();
        public Task<IEnumerable<Character>> GetAvailableNonSlotCharacters(ProjectIdentification projectId) => throw new NotSupportedException();
        public Task<IEnumerable<Character>> GetAllCharacters(int projectId) => throw new NotSupportedException();
        public Task<IEnumerable<Character>> GetActiveTemplateCharacters(int projectId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(IReadOnlyCollection<CharacterIdentification> characterIds) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(ProjectIdentification projectId) => throw new NotSupportedException();
        public void Dispose() { }
    }
}
