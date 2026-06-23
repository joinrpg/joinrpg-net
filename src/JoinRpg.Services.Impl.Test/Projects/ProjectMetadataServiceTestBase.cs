using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Projects;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Projects;

/// <summary>
/// Общая обвязка для тестов сервисов метаданных проекта, собранных поверх реального
/// <see cref="ProjectPropsService"/> и фейков (<see cref="MockedProject"/>, <see cref="FakeUnitOfWork"/>,
/// <see cref="FakeProjectMetadataRepository"/>). Результат операции проверяется через пересобранный
/// <see cref="ProjectInfo"/> (то, что положили в кэш).
/// </summary>
// Члены, протекающие internal-типы (фейки, ProjectPropsService), помечены private protected,
// чтобы публичный (для обнаружения xUnit) базовый класс не «раскрывал» их наружу сборки.
public abstract class ProjectMetadataServiceTestBase
{
    protected readonly MockedProject mock = new();
    private protected readonly FakeUnitOfWork unitOfWork;
    private protected readonly FakeProjectMetadataRepository metadataRepository;

    protected ProjectMetadataServiceTestBase()
    {
        unitOfWork = new FakeUnitOfWork(mock);
        metadataRepository = new FakeProjectMetadataRepository(mock);
    }

    protected ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    /// <summary>Пересобранный после операции снимок метаданных (то, что положили в кэш).</summary>
    protected ProjectInfo Result => metadataRepository.LastPrimed.ShouldNotBeNull();

    private protected FakeCurrentUserAccessor CreateCurrentUser(int? currentUserId = null, bool isAdmin = false)
        => new(currentUserId ?? mock.Master.UserId, isAdmin);

    private protected ProjectPropsService CreatePropsService(FakeCurrentUserAccessor currentUser)
        => new(unitOfWork, currentUser, metadataRepository, NullLogger<ProjectPropsService>.Instance);
}
