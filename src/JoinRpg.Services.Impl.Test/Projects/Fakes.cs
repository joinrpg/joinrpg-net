using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata;
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
            Accommodation = new FakeProjectAccommodationWriteAccess(mock);
            // Снимок ДО, согласованный с текущим Project (как делает боевой репозиторий при загрузке).
            mock.ReInitProjectInfo();
        }

        public Project Project => mock.Project;

        public ProjectInfo ProjectInfo => mock.ProjectInfo;

        public Task<ProjectInfo> Refresh()
        {
            mock.ReInitProjectInfo();
            return Task.FromResult(mock.ProjectInfo);
        }

        public List<object> Removed { get; } = [];

        /// <summary>Всё, что сервис добавил в контекст, в порядке добавления.</summary>
        public List<object> Added { get; } = [];

        public IProjectAccommodationWriteAccess Accommodation { get; }

        public void Add(object entity)
        {
            Added.Add(entity);
            // Имитация relationship fixup EF6: реальный DbContext синхронно связывает добавленную
            // сущность с уже загруженными navigation-коллекциями того же контекста.
            switch (entity)
            {
                case ProjectAccommodationType roomType:
                    mock.AccommodationTypes.Add(roomType);
                    break;
                case ProjectAccommodation room:
                    mock.Rooms.Add(room);
                    room.Inhabitants ??= [];
                    room.ProjectAccommodationType?.ProjectAccommodations.Add(room);
                    break;
                default:
                    break;
            }
        }

        public void Remove(object entity)
        {
            Removed.Add(entity);
            // Имитация relationship fixup EF6: реальный DbContext синхронно убирает удалённую
            // сущность из уже загруженных navigation-коллекций того же контекста.
            if (entity is ProjectAcl acl)
            {
                _ = mock.Project.ProjectAcls.Remove(acl);
            }
            if (entity is DataModel.ProjectRolesList rolesList)
            {
                _ = mock.Project.ProjectRolesLists.Remove(rolesList);
            }
            if (entity is ProjectFeeSetting feeSetting)
            {
                _ = mock.Project.ProjectFeeSettings.Remove(feeSetting);
            }
            if (entity is ProjectAccommodationType roomType)
            {
                _ = mock.AccommodationTypes.Remove(roomType);
            }
            if (entity is ProjectAccommodation room)
            {
                _ = mock.Rooms.Remove(room);
                _ = room.ProjectAccommodationType?.ProjectAccommodations.Remove(room);
            }
        }
    }
}

/// <summary>
/// Загрузчики поселения поверх <see cref="MockedProject"/>: отдают ровно те сущности, которые
/// боевой репозиторий взял бы из <c>DbContext</c> хэндла, и так же ограничены проектом мока.
/// </summary>
internal sealed class FakeProjectAccommodationWriteAccess(MockedProject mock) : IProjectAccommodationWriteAccess
{
    public Task<ProjectAccommodationType?> LoadRoomType(int roomTypeId)
        => Task.FromResult(mock.AccommodationTypes.SingleOrDefault(t => t.Id == roomTypeId));

    public Task<ProjectAccommodation?> LoadRoom(int roomId)
        => Task.FromResult(mock.Rooms.SingleOrDefault(r => r.Id == roomId));

    public Task<IReadOnlyCollection<ProjectAccommodation>> LoadOccupiedRooms(int? roomTypeId)
        => Task.FromResult<IReadOnlyCollection<ProjectAccommodation>>(
            [.. mock.Rooms
                .Where(r => r.Inhabitants.Any())
                .Where(r => roomTypeId is null || r.AccommodationTypeId == roomTypeId)]);

    public Task<IReadOnlyCollection<AccommodationRequest>> LoadAccommodationRequests(IReadOnlyCollection<int> requestIds)
        => Task.FromResult<IReadOnlyCollection<AccommodationRequest>>(
            [.. mock.AccommodationRequests.Where(r => requestIds.Contains(r.Id))]);
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

/// <summary>Записывает вызовы <see cref="IClaimService.SetResponsible"/> вместо реального изменения заявки.</summary>
internal sealed class FakeClaimService : IClaimService
{
    public List<(ClaimIdentification ClaimId, UserIdentification ResponsibleMasterId)> ResponsibleChanges { get; } = [];

    public Task SetResponsible(ClaimIdentification claimId, UserIdentification responsibleMasterId)
    {
        ResponsibleChanges.Add((claimId, responsibleMasterId));
        return Task.CompletedTask;
    }

    public Task<ClaimIdentification> AddClaimFromUser(CharacterIdentification characterId, string claimText, FieldLayerContainer fields, bool sensitiveDataAllowed) => throw new NotSupportedException();
    public Task<ClaimIdentification> AddClaimFromMaster(CharacterIdentification characterId, UserIdentification userId, string commentText, FieldLayerContainer fields) => throw new NotSupportedException();
    public Task AddComment(ClaimIdentification claimId, int? parentCommentId, bool isVisibleToPlayer, string commentText, FinanceOperationAction financeAction) => throw new NotSupportedException();
    public Task ApproveByMaster(ClaimIdentification claimId, string commentText) => throw new NotSupportedException();
    public Task DeclineByMaster(ClaimIdentification claimId, ClaimDenialReason claimDenialStatus, string commentText, bool deleteCharacter) => throw new NotSupportedException();
    public Task DeclineByPlayer(ClaimIdentification claimId, string commentText) => throw new NotSupportedException();
    public Task OnHoldByMaster(ClaimIdentification claimId, string commentText) => throw new NotSupportedException();
    public Task RestoreByMaster(ClaimIdentification claimId, string commentText, CharacterIdentification characterId) => throw new NotSupportedException();
    public Task MoveByMaster(ClaimIdentification claimId, string commentText, CharacterIdentification characterId) => throw new NotSupportedException();
    public Task UpdateReadCommentWatermark(int projectId, int commentDiscussionId, int maxCommentId) => throw new NotSupportedException();
    public Task SaveFieldsFromClaim(ClaimIdentification claimId, FieldLayerContainer fieldsToSet) => throw new NotSupportedException();
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
