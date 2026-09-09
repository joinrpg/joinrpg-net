using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// Простая заглушка <see cref="IProjectUriLocator"/> для bUnit-тестов:
/// в репозитории нет библиотеки для мокирования (Moq/NSubstitute), поэтому пишем вручную.
/// </summary>
internal sealed class FakeProjectUriLocator : IProjectUriLocator
{
    private static Uri UriFor(ProjectIdentification projectId, string suffix)
        => new($"https://example.org/projects/{projectId.Value}/{suffix}");

    public Uri GetMyClaimUri(ProjectIdentification projectId) => UriFor(projectId, "my-claim");

    public Uri GetAddClaimUri(ProjectIdentification projectId) => UriFor(projectId, "add-claim");

    public Uri GetCreatePlotUri(ProjectIdentification projectId) => UriFor(projectId, "create-plot");

    public Uri GetRolesListUri(ProjectIdentification projectId) => UriFor(projectId, "roles");

    public Uri GetCaptainCabinetUri(ProjectIdentification projectId) => UriFor(projectId, "captain-cabinet");
}
