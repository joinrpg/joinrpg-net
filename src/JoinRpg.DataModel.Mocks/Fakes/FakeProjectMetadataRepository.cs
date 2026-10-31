using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;

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
    // Именно с уточнением namespace: в JoinRpg.DataModel есть своя ProjectDetails (EF-сущность).
    private readonly Func<DomainTypes.ProjectMetadata.ProjectDetails>? details;

    public FakeProjectMetadataRepository(MockedProject mock)
    {
        projectInfo = () => mock.ProjectInfo;
        // Ссылки и описания методов — как в настоящем репозитории: по активным методам мока
        details = () => new DomainTypes.ProjectMetadata.ProjectDetails(
            mock.ProjectInfo,
            new MarkdownString(""),
            new MarkdownString(""),
            [],
            false,
            new MarkdownString(""),
            [.. mock.Project.PaymentTypes
                .Where(pt => pt.IsActive)
                .Select(pt => new PaymentTypePlayerInfo(
                    new PaymentTypeIdentification(mock.Project.ProjectId, pt.PaymentTypeId),
                    pt.Name,
                    // Как в настоящем репозитории: строка сущности — в Uri, кривая — в null
                    Uri.TryCreate(pt.PaymentLink, UriKind.Absolute, out var paymentLink) ? paymentLink : null,
                    pt.LinkKind,
                    pt.Description))]);
    }

    public FakeProjectMetadataRepository(ProjectInfo projectInfo) => this.projectInfo = () => projectInfo;

    /// <summary>Последнее, что положили в кеш — тесты пути записи проверяют, что это произошло.</summary>
    public ProjectInfo? LastPrimed { get; private set; }

    public Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache = false)
        => Task.FromResult(projectInfo());

    public Task<DomainTypes.ProjectMetadata.ProjectDetails> GetProjectDetails(ProjectInfo projectInfo)
        => Task.FromResult(details is not null
            ? details()
            : new DomainTypes.ProjectMetadata.ProjectDetails(projectInfo, new MarkdownString(""), new MarkdownString(""), [], false, new MarkdownString(""), []));

    public void PrimeCache(ProjectInfo projectInfo) => LastPrimed = projectInfo;
}
