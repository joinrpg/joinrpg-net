using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Advertisement;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>Репозиторий проектов, умеющий только отдать список заблокированных проектов.</summary>
internal sealed class FakeProjectRepository : IProjectRepository
{
    public IReadOnlyCollection<ProjectWithUpdateDateDto> BlockedProjects { get; init; } = [];

    public Task<IReadOnlyCollection<ProjectWithUpdateDateDto>> GetBlockedProjects() => Task.FromResult(BlockedProjects);

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
    public Task<IReadOnlyCollection<FormerMasterCandidate>> GetFormerMasterCandidates() => throw new NotSupportedException();
    public Task<ProjectPersonalizedInfo[]> GetPersonalizedProjectsBySpecification(PersonalizedProjectListSpecification projectListSpecification) => throw new NotSupportedException();
    public Task<ProjectShortInfo[]> GetProjectsBySpecification(ProjectListSpecification projectListSpecification) => throw new NotSupportedException();
    public Task<ProjectPersonalizedInfo[]> GetProjectsByIds(UserIdentification? userId, ProjectIdentification[] ids) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<ProjectAdvertisementCandidate>> GetPublicProjectsOpenForHotRoleAdvertisement() => throw new NotSupportedException();
    public Task<IReadOnlyCollection<ProjectAdvertisementCandidate>> GetPublicProjectsOpenedForClaimsInLastWeek() => throw new NotSupportedException();
}
