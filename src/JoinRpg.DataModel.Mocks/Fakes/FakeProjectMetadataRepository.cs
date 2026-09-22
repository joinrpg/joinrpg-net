using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DataModel.Mocks.Fakes;

/// <summary>
/// Репозиторий метаданных поверх мока — общий для всех тестовых проектов.
/// </summary>
/// <remarks>
/// Версия с <see cref="MockedProject"/> предпочтительнее: <c>ReInitProjectInfo</c> и
/// <c>CreateField</c> подменяют экземпляр <see cref="ProjectInfo"/>, и зафиксированный при
/// конструировании снимок разъедется с тем, что видит тест. Перегрузка с готовым
/// <see cref="ProjectInfo"/> — для тестов, которые собирают его сами.
/// </remarks>
public sealed class FakeProjectMetadataRepository : IProjectMetadataRepository
{
    private readonly Func<ProjectInfo> projectInfo;

    public FakeProjectMetadataRepository(MockedProject mock) => projectInfo = () => mock.ProjectInfo;

    public FakeProjectMetadataRepository(ProjectInfo projectInfo) => this.projectInfo = () => projectInfo;

    /// <summary>Последнее, что положили в кеш — тесты пути записи проверяют, что это произошло.</summary>
    public ProjectInfo? LastPrimed { get; private set; }

    public Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache = false)
        => Task.FromResult(projectInfo());

    public Task<DomainTypes.ProjectMetadata.ProjectDetails> GetProjectDetails(ProjectIdentification projectId)
        // Именно с уточнением namespace: в JoinRpg.DataModel есть своя ProjectDetails (EF-сущность).
        => Task.FromResult(new DomainTypes.ProjectMetadata.ProjectDetails(projectInfo(), new MarkdownString(""), [], false));

    public void PrimeCache(ProjectInfo projectInfo) => LastPrimed = projectInfo;
}
