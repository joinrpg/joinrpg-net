using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.Test.Projects;

public class BlockedProjectsReminderJobTest
{
    private static readonly DateTimeOffset Tuesday = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Wednesday = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeNotificationService notificationService = new();

    private BlockedProjectsReminderJob CreateJob(
        IProjectRepository projectRepository,
        IProjectMetadataRepository metadataRepository,
        DateTimeOffset now)
        => new(
            projectRepository,
            metadataRepository,
            new MasterEmailService(notificationService, metadataRepository, new FakeVirtualUsersService(), new FakeUserRepository()),
            new FixedTimeProvider(now),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<BlockedProjectsReminderJob>.Instance);

    private static MockedProject CreateBlockedProject(int projectId)
    {
        var mock = new MockedProject { Project = { ProjectId = projectId } };
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();
        mock.ProjectInfo.ProjectStatus.ShouldBe(ProjectLifecycleStatus.Blocked);
        return mock;
    }

    [Fact]
    public async Task RunOnce_NotTuesday_SendsNothing()
    {
        var job = CreateJob(
            new FakeProjectRepository { BlockedProjects = [new() { ProjectId = 1, ProjectName = "P", LastUpdated = DateTime.UtcNow }] },
            new StubMetadataRepository(_ => throw new NotSupportedException("Вне вторника метаданные не читаются")),
            Wednesday);

        await job.RunOnce(CancellationToken.None);

        notificationService.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunOnce_Tuesday_SendsReminderForEveryBlockedProject()
    {
        var mocks = new[] { CreateBlockedProject(1), CreateBlockedProject(2) };
        var job = CreateJob(
            new FakeProjectRepository
            {
                BlockedProjects =
                [
                    new() { ProjectId = 1, ProjectName = "P1", LastUpdated = DateTime.UtcNow },
                    new() { ProjectId = 2, ProjectName = "P2", LastUpdated = DateTime.UtcNow },
                ],
            },
            new StubMetadataRepository(id => Task.FromResult(mocks.Single(m => m.Project.ProjectId == id.Value).ProjectInfo)),
            Tuesday);

        await job.RunOnce(CancellationToken.None);

        notificationService.Queued.Count.ShouldBe(2);
        notificationService.Queued.Select(n => n.EntityReference).ShouldBe([new ProjectIdentification(1), new ProjectIdentification(2)]);
        notificationService.Queued.ShouldAllBe(n => n.NotificationClass == NotificationClass.MasterProject);
    }

    [Fact]
    public async Task RunOnce_Tuesday_SkipsProjectThatIsNotBlockedAnymore()
    {
        var unblocked = new MockedProject { Project = { ProjectId = 1 } }; // статус по умолчанию — ActiveClaimsOpen
        var job = CreateJob(
            new FakeProjectRepository { BlockedProjects = [new() { ProjectId = 1, ProjectName = "P", LastUpdated = DateTime.UtcNow }] },
            new StubMetadataRepository(_ => Task.FromResult(unblocked.ProjectInfo)),
            Tuesday);

        await job.RunOnce(CancellationToken.None);

        notificationService.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunOnce_Tuesday_FailureOnOneProject_StillSendsForOthers()
    {
        var blocked = CreateBlockedProject(2);
        var job = CreateJob(
            new FakeProjectRepository
            {
                BlockedProjects =
                [
                    new() { ProjectId = 1, ProjectName = "Упавший", LastUpdated = DateTime.UtcNow },
                    new() { ProjectId = 2, ProjectName = "Живой", LastUpdated = DateTime.UtcNow },
                ],
            },
            new StubMetadataRepository(id => id.Value == 1
                ? throw new NotSupportedException("База недоступна")
                : Task.FromResult(blocked.ProjectInfo)),
            Tuesday);

        await job.RunOnce(CancellationToken.None);

        var letter = notificationService.Queued.ShouldHaveSingleItem();
        letter.EntityReference.ShouldBe(new ProjectIdentification(2));
    }

    private sealed class StubMetadataRepository(Func<ProjectIdentification, Task<ProjectInfo>> getMetadata) : IProjectMetadataRepository
    {
        public Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache = false) => getMetadata(projectId);
        public Task<DomainTypes.ProjectMetadata.ProjectDetails> GetProjectDetails(ProjectInfo projectInfo) => throw new NotSupportedException();
        public void PrimeCache(ProjectInfo projectInfo) => throw new NotSupportedException();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
