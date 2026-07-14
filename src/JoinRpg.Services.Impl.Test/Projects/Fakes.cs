using System.Data.Entity;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.AdminTools;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Interfaces.Finances;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Interfaces;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Subscribe;

namespace JoinRpg.Services.Impl.Test.Projects;

/// <summary>
/// Write-репозиторий поверх <see cref="MockedProject"/>: отдаёт согласованную пару
/// <see cref="Project"/>/<see cref="ProjectInfo"/> и пересобирает снимок через тот же
/// <c>CreateInfoFromProject</c>, что и боевой код.
/// </summary>
internal sealed class FakeProjectMetadataWriteRepository(MockedProject mock) : IProjectMetadataWriteRepository
{
    public Task<IProjectMetadataUpdateHandle> LoadProjectForUpdate(ProjectIdentification projectId)
        => Task.FromResult<IProjectMetadataUpdateHandle>(new Handle(mock));

    private sealed class Handle : IProjectMetadataUpdateHandle
    {
        private readonly MockedProject mock;

        public Handle(MockedProject mock)
        {
            this.mock = mock;
            // Снимок ДО, согласованный с текущим Project (как делает боевой репозиторий при загрузке).
            mock.ReInitProjectInfo();
        }

        public Project Project => mock.Project;

        public ProjectInfo ProjectInfo => mock.ProjectInfo;

        public ProjectInfo Refresh()
        {
            mock.ReInitProjectInfo();
            return mock.ProjectInfo;
        }

        public List<object> Removed { get; } = [];

        public void Remove(object entity)
        {
            Removed.Add(entity);
            // Имитация relationship fixup EF6: реальный DbContext синхронно убирает удалённую
            // сущность из уже загруженных navigation-коллекций того же контекста.
            if (entity is ProjectAcl acl)
            {
                _ = mock.Project.ProjectAcls.Remove(acl);
            }
        }
    }
}

/// <summary>
/// Read-репозиторий поверх <see cref="MockedProject"/>: запоминает последний примированный
/// <see cref="ProjectInfo"/> и отдаёт текущий снимок мока.
/// </summary>
internal sealed class FakeProjectMetadataRepository(MockedProject mock) : IProjectMetadataRepository
{
    public ProjectInfo? LastPrimed { get; private set; }

    public void PrimeCache(ProjectInfo projectInfo) => LastPrimed = projectInfo;

    public Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache = false)
        => Task.FromResult(mock.ProjectInfo);

    public Task<JoinRpg.DomainTypes.ProjectMetadata.ProjectDetails> GetProjectDetails(ProjectIdentification projectId)
        => throw new NotSupportedException();
}

internal sealed class FakeCurrentUserAccessor(int userId, bool isAdmin = false) : ICurrentUserAccessor
{
    public int? UserIdOrDefault => userId;
    public UserDisplayName DisplayName => new("Test", null);
    public bool IsAdmin => isAdmin;
    public AvatarIdentification? Avatar => null;
}

internal sealed class FakeUnitOfWork(MockedProject mock) : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task SaveChangesAsync()
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }

    public IProjectMetadataWriteRepository GetProjectMetadataWriteRepository()
        => new FakeProjectMetadataWriteRepository(mock);

    public DbSet<T> GetDbSet<T>() where T : class => throw new NotSupportedException();
    public IUserRepository GetUsersRepository() => throw new NotSupportedException();
    public IProjectRepository GetProjectRepository() => throw new NotSupportedException();
    public IClaimsRepository GetClaimsRepository() => throw new NotSupportedException();
    public IPlotRepository GetPlotRepository() => throw new NotSupportedException();
    public IForumRepository GetForumRepository() => throw new NotSupportedException();
    public ICharacterRepository GetCharactersRepository() => throw new NotSupportedException();
    public IAccommodationRepository GetAccomodationRepository() => throw new NotSupportedException();
    public IKogdaIgraRepository GetKogdaIgraRepository() => throw new NotSupportedException();
    public IFinanceOperationsRepository GetFinanceOperationsRepositoryRepository() => throw new NotSupportedException();

    public void Dispose() { }

    public Task<int> ExecuteSqlCommandAsync(string sql) => throw new NotImplementedException();
}

/// <summary>Записывает поставленные в очередь уведомления для проверки в тестах.</summary>
internal sealed class FakeNotificationService : INotificationService
{
    public List<NotificationEvent> Queued { get; } = [];

    public Task QueueNotification(NotificationEvent notificationMessage)
    {
        Queued.Add(notificationMessage);
        return Task.CompletedTask;
    }

    public Task QueueDirectNotification(NotificationEvent notificationMessage, NotificationChannel directChannel)
    {
        Queued.Add(notificationMessage);
        return Task.CompletedTask;
    }
}

internal sealed class FakeVirtualUsersService : IVirtualUsersService
{
    public User PaymentsUser => throw new NotSupportedException();
    public User RobotUser => throw new NotSupportedException();
    public UserIdentification RobotUserId => new(int.MaxValue);
}

/// <summary>Отдаёт заранее заготовленные заявки по (ProjectId, UserId) отв. мастера.</summary>
internal sealed class FakeClaimsRepository : IClaimsRepository
{
    public Dictionary<(int ProjectId, int UserId), List<Claim>> ClaimsByResponsibleMaster { get; } = [];

    public Task<IReadOnlyCollection<Claim>> GetClaimsForMaster(int projectId, int userId, ClaimStatusSpec status)
        => Task.FromResult<IReadOnlyCollection<Claim>>(
            ClaimsByResponsibleMaster.TryGetValue((projectId, userId), out var claims) ? claims : []);

    public void Dispose() { }

    public Task<IReadOnlyCollection<Claim>> GetClaims(int projectId, ClaimStatusSpec status) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(UserIdentification userId, ClaimStatusSpec status) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(ProjectIdentification projectId, UserIdentification userId, ClaimStatusSpec status) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(IReadOnlyCollection<ClaimIdentification> claimIds) => throw new NotSupportedException();
    public Task<Claim?> GetClaim(ClaimIdentification claimId) => throw new NotSupportedException();
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

/// <summary>Записывает вызовы <see cref="IClaimService.SetResponsible"/> вместо реального изменения заявки.</summary>
internal sealed class FakeClaimService : IClaimService
{
    public List<(ClaimIdentification ClaimId, UserIdentification ResponsibleMasterId)> ResponsibleChanges { get; } = [];

    public Task SetResponsible(ClaimIdentification claimId, UserIdentification responsibleMasterId)
    {
        ResponsibleChanges.Add((claimId, responsibleMasterId));
        return Task.CompletedTask;
    }

    public Task<ClaimIdentification> AddClaimFromUser(CharacterIdentification characterId, string claimText, IReadOnlyDictionary<int, string?> fields, bool sensitiveDataAllowed) => throw new NotSupportedException();
    public Task<ClaimIdentification> AddClaimFromMaster(CharacterIdentification characterId, UserIdentification userId, string commentText, IReadOnlyDictionary<int, string?> fields) => throw new NotSupportedException();
    public Task AddComment(ClaimIdentification claimId, int? parentCommentId, bool isVisibleToPlayer, string commentText, FinanceOperationAction financeAction) => throw new NotSupportedException();
    public Task ApproveByMaster(ClaimIdentification claimId, string commentText) => throw new NotSupportedException();
    public Task DeclineByMaster(ClaimIdentification claimId, ClaimDenialReason claimDenialStatus, string commentText, bool deleteCharacter) => throw new NotSupportedException();
    public Task DeclineByPlayer(ClaimIdentification claimId, string commentText) => throw new NotSupportedException();
    public Task OnHoldByMaster(ClaimIdentification claimId, string commentText) => throw new NotSupportedException();
    public Task RestoreByMaster(ClaimIdentification claimId, string commentText, CharacterIdentification characterId) => throw new NotSupportedException();
    public Task MoveByMaster(ClaimIdentification claimId, string commentText, CharacterIdentification characterId) => throw new NotSupportedException();
    public Task UpdateReadCommentWatermark(int projectId, int commentDiscussionId, int maxCommentId) => throw new NotSupportedException();
    public Task SaveFieldsFromClaim(ClaimIdentification claimId, IReadOnlyDictionary<int, string?> newFieldValue) => throw new NotSupportedException();
    public Task CheckInClaim(ClaimIdentification claimId, int money) => throw new NotSupportedException();
    public Task<int> MoveToSecondRole(ClaimIdentification claimId, CharacterIdentification characterId, string secondRoleCommentText) => throw new NotSupportedException();
    public Task<AccommodationRequest> SetAccommodationType(int projectId, int claimId, int accommodationTypeId) => throw new NotSupportedException();
    public Task<AccommodationRequest?> LeaveAccommodationGroupAsync(int projectId, int claimId) => throw new NotSupportedException();
    public Task ConcealComment(int projectId, int commentId, int commentDiscussionId) => throw new NotSupportedException();
    public Task AllowSensitiveData(ClaimIdentification projectId) => throw new NotSupportedException();
    public Task AcceptInvitation(ClaimIdentification claimId, string commentText, bool sensitiveDataAllowed) => throw new NotSupportedException();
    public Task<ClaimIdentification> SystemEnsureClaim(ProjectIdentification donateProjectId) => throw new NotSupportedException();
}

/// <summary>Записывает вызовы <see cref="IGameSubscribeService.RemoveAllSubscriptions"/>.</summary>
internal sealed class FakeGameSubscribeService : IGameSubscribeService
{
    public List<(ProjectIdentification ProjectId, UserIdentification UserId)> RemoveAllSubscriptionsCalls { get; } = [];

    public Task RemoveAllSubscriptions(ProjectIdentification projectId, UserIdentification userId)
    {
        RemoveAllSubscriptionsCalls.Add((projectId, userId));
        return Task.CompletedTask;
    }

    public Task UpdateSubscribeForGroup(SubscribeForGroupRequest request) => throw new NotSupportedException();
    public Task RemoveSubscribe(RemoveSubscribeRequest request) => throw new NotSupportedException();
    public Task SubscribeClaimToUser(ClaimIdentification claimId) => throw new NotSupportedException();
    public Task UnsubscribeClaimToUser(ClaimIdentification claimId) => throw new NotSupportedException();
}
