using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Advertisement;
using JoinRpg.Interfaces;
using JoinRpg.WebPortal.Managers.Projects;

namespace JoinRpg.WebPortal.Managers.Test.Projects;

/// <summary>
/// <see cref="ProjectApiViewService"/> — общий слой над проектами для x-api и MCP (ADR012).
/// </summary>
public class ProjectApiViewServiceTests
{
    private MockedProject Mock { get; } = new MockedProject();

    private ProjectApiViewService CreateService(int userId) =>
        new(
            new FakeProjectMetadataRepository(Mock.ProjectInfo),
            new FakeProjectRepository(Mock),
            new FakeCurrentUserAccessor { UserIdentification = new UserIdentification(userId) });

    [Fact]
    public async Task GetOverview_NonMaster_ThrowsNoAccessToProjectException()
    {
        var service = CreateService(userId: 12345);

        await Should.ThrowAsync<NoAccessToProjectException>(() => service.GetOverview(Mock.ProjectInfo.ProjectId));
    }

    [Fact]
    public async Task GetOverview_Master_ReturnsGroupsAndFields()
    {
        var service = CreateService(Mock.Master.UserId);

        var overview = await service.GetOverview(Mock.ProjectInfo.ProjectId);

        overview.Groups.ShouldContain(g => g.CharacterGroupId == Mock.Group.CharacterGroupId);
        overview.Fields.ShouldContain(f => f.ProjectFieldId == Mock.CharacterFieldInfo.Id.ProjectFieldId);
    }

    [Fact]
    public async Task ListMasterProjects_ReturnsOnlyProjectsWithMasterAccess()
    {
        var service = CreateService(Mock.Master.UserId);

        var projects = await service.ListMasterProjects(new UserIdentification(Mock.Master.UserId));

        projects.ShouldContain(p => p.ProjectId == Mock.ProjectInfo.ProjectId.Value);
        projects.ShouldNotContain(p => p.ProjectName == "Без доступа");
    }

    private sealed class FakeProjectMetadataRepository(ProjectInfo projectInfo) : IProjectMetadataRepository
    {
        public Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache = false)
            => Task.FromResult(projectInfo);

        public Task<JoinRpg.DomainTypes.ProjectMetadata.ProjectDetails> GetProjectDetails(ProjectIdentification projectId)
            => Task.FromResult(new JoinRpg.DomainTypes.ProjectMetadata.ProjectDetails(new MarkdownString(""), [], false));

        public void PrimeCache(ProjectInfo projectInfo) { }
    }

    private sealed class FakeProjectRepository(MockedProject mock) : IProjectRepository
    {
        public Task<ProjectPersonalizedInfo[]> GetPersonalizedProjectsBySpecification(PersonalizedProjectListSpecification projectListSpecification)
            => Task.FromResult<ProjectPersonalizedInfo[]>([
                new ProjectPersonalizedInfo(mock.ProjectInfo.ProjectId, ProjectLifecycleStatus.ActiveClaimsOpen, false, new ProjectName("Мой проект"), 0, false, true, null, false),
                new ProjectPersonalizedInfo(new ProjectIdentification(mock.ProjectInfo.ProjectId.Value + 1), ProjectLifecycleStatus.ActiveClaimsOpen, false, new ProjectName("Без доступа"), 0, false, false, null, false),
            ]);

        [Obsolete]
        public Task<Project> GetProjectAsync(int project) => throw new NotImplementedException();
        public Task<Project?> GetProjectWithFieldsAsync(int project) => throw new NotImplementedException();
        public Task<CharacterGroup?> GetGroupAsync(CharacterGroupIdentification characterGroupId) => throw new NotImplementedException();
        public Task<CharacterGroup?> LoadGroupWithTreeAsync(int projectId, int? characterGroupId = null) => throw new NotImplementedException();
        public Task<IList<CharacterGroup>> LoadGroups(IReadOnlyCollection<CharacterGroupIdentification> groupIds) => throw new NotImplementedException();
        public Task<Project> GetProjectWithFinances(int projectid) => throw new NotImplementedException();
        public Task<Project> GetProjectForFinanceSetup(int projectid) => throw new NotImplementedException();
        public Task<ICollection<Character>> GetCharacterByGroups(IReadOnlyCollection<CharacterGroupIdentification> characterGroupIds) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<ProjectWithUpdateDateDto>> GetStaleProjects(DateTime inActiveSince) => throw new NotImplementedException();
        public Task<ProjectShortInfo[]> GetProjectsBySpecification(ProjectListSpecification projectListSpecification) => throw new NotImplementedException();
        public Task<ProjectPersonalizedInfo[]> GetProjectsByIds(UserIdentification? userId, ProjectIdentification[] ids) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<ProjectAdvertisementCandidate>> GetPublicProjectsOpenForHotRoleAdvertisement() => throw new NotImplementedException();
        public void Dispose() { }
    }

    private sealed class FakeCurrentUserAccessor : ICurrentUserAccessor
    {
        public UserIdentification UserIdentification { get; set; } = new UserIdentification(0);
        public int? UserIdOrDefault => UserIdentification.Value;
        public UserDisplayName DisplayName => new UserDisplayName("Test", null);
        public bool IsAdmin => false;
        public AvatarIdentification? Avatar => null;
    }
}
