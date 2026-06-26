using System.Data.Entity;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.AdminTools;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Interfaces.Finances;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Interfaces;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.Services.Interfaces;

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
