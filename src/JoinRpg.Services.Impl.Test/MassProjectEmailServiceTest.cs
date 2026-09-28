using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Email;

namespace JoinRpg.Services.Impl.Test;

public class MassProjectEmailServiceTest
{
    private readonly MockedProject mock = new();
    private readonly FakeNotificationService notificationService = new();

    private ClaimWithPlayer Claim => new()
    {
        ClaimId = new ClaimIdentification(mock.Project.ProjectId, 1),
        CharacterId = new CharacterIdentification(mock.Project.ProjectId, 1),
        CharacterName = "CharacterName",
        ExtraNicknames = "",
        Player = new UserInfoHeader(new UserIdentification(mock.Player.UserId), new UserDisplayName("Player", null)),
        ResponsibleMasterUserId = new UserIdentification(mock.Master.UserId),
    };

    private MassProjectEmailService CreateService()
        => new(
            new FakeMassMailClaimsRepository(Claim),
            new FakeProjectMetadataRepository(mock),
            notificationService,
            new FakeCurrentUserAccessor(mock.Master.UserId),
            new FakeClaimUriLocator());

    [Fact]
    public async Task MassMail_HeaderStartsWithProjectName()
    {
        await CreateService().MassMail(
            [Claim.ClaimId],
            new MarkdownDbValue("Текст рассылки"),
            "Тема рассылки",
            alsoMailToMasters: false);

        notificationService.Queued.Single().Header.ShouldBe("Mocked project: Тема рассылки");
    }

    [Fact]
    public async Task MassMail_ToMasters_HeaderStartsWithProjectName()
    {
        await CreateService().MassMail(
            [Claim.ClaimId],
            new MarkdownDbValue("Текст рассылки"),
            "Тема рассылки",
            alsoMailToMasters: true);

        notificationService.Queued.Select(n => n.Header)
            .ShouldAllBe(header => header == "Mocked project: Тема рассылки");
    }

    /// <summary>Отдаёт заранее заготовленные заявки — рассылке больше ничего от репозитория не нужно.</summary>
    private sealed class FakeMassMailClaimsRepository(params ClaimWithPlayer[] claims) : IClaimsRepository
    {
        public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(IReadOnlyCollection<ClaimIdentification> claimIds)
            => Task.FromResult<IReadOnlyCollection<ClaimWithPlayer>>([.. claims.Where(c => claimIds.Contains(c.ClaimId))]);

        public void Dispose() { }

        public Task<IReadOnlyCollection<Claim>> GetClaims(int projectId, ClaimStatusSpec status) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForMaster(int projectId, int userId, ClaimStatusSpec status) => throw new NotSupportedException();
        public Task<Claim?> GetClaim(ClaimIdentification claimId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(UserIdentification userId, ClaimStatusSpec status) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<MyClaimShortInfo>> GetMyActiveClaimsInActiveProjects(UserIdentification userId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(ProjectIdentification projectId, UserIdentification userId, ClaimStatusSpec status) => throw new NotSupportedException();
        public Task<Claim?> GetClaimWithDetails(ClaimIdentification claimId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForGroups(ProjectIdentification projectId, ClaimStatusSpec active, CharacterGroupIdentification[] characterGroupsIds) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(IReadOnlyCollection<CharacterGroupIdentification> characterGroupsIds, ClaimStatusSpec spec) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(int projectId, ClaimStatusSpec claimStatusSpec, int userId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimsHeadersForPlayer(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec, UserIdentification userId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ClaimCountByMaster>> GetClaimsCountByMasters(int projectId, ClaimStatusSpec claimStatusSpec) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(ProjectIdentification projectId, ClaimStatusSpec approved) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForRoomType(int projectId, ClaimStatusSpec claimStatusSpec, int? roomTypeId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForMoneyTransfersListAsync(int projectId, ClaimStatusSpec claimStatusSpec) => throw new NotSupportedException();
        public Task<Dictionary<int, int>> GetUnreadDiscussionsForClaims(int projectId, ClaimStatusSpec claimStatusSpec, int userId, bool hasMasterAccess) => throw new NotSupportedException();
    }

    private sealed class FakeClaimUriLocator : IUriLocator<ClaimIdentification>
    {
        public Uri GetUri(ClaimIdentification target) => new($"https://joinrpg.ru/{target.ProjectId.Value}/claim/{target.ClaimId}");
    }
}
