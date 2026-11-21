using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Advertisement;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JoinRpg.Portal.Test.Infrastructure.HealthChecks;

/// <summary>
/// Регрессия: health-чек «Project load» грузил полный список активных проектов с агрегатом
/// дат последнего обновления. На проде это 1–2 секунды на каждый вызов, и проба не укладывалась
/// в таймаут — поды не становились Ready, деплой вставал. Чек обязан делать дешёвый запрос.
/// </summary>
public class HealthCheckLoadProjectsTest
{
    [Fact]
    public async Task ReturnsHealthyWhenActiveProjectsLoadable()
    {
        var healthCheck = new HealthCheckLoadProjects(new StubProjectRepository());

        var result = await healthCheck.CheckHealthAsync(context: new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task ReturnsUnhealthyWhenRepositoryFails()
    {
        var healthCheck = new HealthCheckLoadProjects(new StubProjectRepository(fail: true));

        var result = await healthCheck.CheckHealthAsync(context: new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    private sealed class StubProjectRepository(bool fail = false) : IProjectRepository
    {
        public Task<bool> HasActiveProjects()
            => fail
                ? Task.FromException<bool>(new InvalidOperationException("БД недоступна"))
                : Task.FromResult(true);

        public Task<ProjectShortInfo[]> GetProjectsBySpecification(ProjectListSpecification projectListSpecification)
            => throw new InvalidOperationException("Чек не должен грузить полный список проектов");

        public void Dispose() { }

        public Task<Project> GetProjectAsync(int project) => throw new NotSupportedException();
        public Task<Project> GetProjectForMarkdownRendering(ProjectIdentification projectId) => throw new NotSupportedException();
        public Task<CharacterGroup?> GetGroupAsync(CharacterGroupIdentification characterGroupId) => throw new NotSupportedException();
        public Task<CharacterGroup?> LoadGroupWithTreeAsync(int projectId, int? characterGroupId = null) => throw new NotSupportedException();
        public Task<IList<CharacterGroup>> LoadGroups(IReadOnlyCollection<CharacterGroupIdentification> groupIds) => throw new NotSupportedException();
        public Task<Project> GetProjectWithFinances(int projectid) => throw new NotSupportedException();
        public Task<Project> GetProjectForFinanceSetup(int projectid) => throw new NotSupportedException();
        public Task<Guid?> GetMasterExportToken(ProjectIdentification projectId, UserIdentification userId) => throw new NotSupportedException();
        public Task<bool> IsActiveMasterExportToken(ProjectIdentification projectId, Guid token) => throw new NotSupportedException();
        public Task<ICollection<Character>> GetCharacterByGroups(IReadOnlyCollection<CharacterGroupIdentification> characterGroupIds) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ProjectWithUpdateDateDto>> GetStaleProjects(DateTime inActiveSince) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ProjectWithUpdateDateDto>> GetBlockedProjects() => throw new NotSupportedException();
        public Task<IReadOnlyCollection<FormerMasterCandidate>> GetFormerMasterCandidates() => throw new NotSupportedException();
        public Task<ProjectPersonalizedInfo[]> GetPersonalizedProjectsBySpecification(PersonalizedProjectListSpecification projectListSpecification) => throw new NotSupportedException();
        public Task<ProjectPersonalizedInfo[]> GetProjectsByIds(UserIdentification? userId, ProjectIdentification[] ids) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ProjectAdvertisementCandidate>> GetPublicProjectsOpenForHotRoleAdvertisement() => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ProjectAdvertisementCandidate>> GetPublicProjectsOpenedForClaimsInLastWeek() => throw new NotSupportedException();
    }
}
