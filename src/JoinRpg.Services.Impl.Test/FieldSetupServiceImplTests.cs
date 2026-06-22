using System.Data.Entity;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.AdminTools;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Interfaces.Finances;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Interfaces;

namespace JoinRpg.Services.Impl.Test;

public class FieldSetupServiceImplTests
{
    private class StubCurrentUserAccessor : ICurrentUserAccessor
    {
        public int? UserIdOrDefault => 2;
        public UserDisplayName DisplayName => new("Master", FullName: null);
        public bool IsAdmin => false;
        public AvatarIdentification? Avatar => null;
    }

    private class StubUnitOfWork : IUnitOfWork
    {
        private readonly IProjectRepository _projectRepository;
        public StubUnitOfWork(IProjectRepository projectRepository) => _projectRepository = projectRepository;

        public Task SaveChangesAsync() => Task.CompletedTask;
        public void Dispose() { }
        public DbSet<T> GetDbSet<T>() where T : class => throw new NotImplementedException();
        public IUserRepository GetUsersRepository() => throw new NotImplementedException();
        public IProjectRepository GetProjectRepository() => _projectRepository;
        public IClaimsRepository GetClaimsRepository() => throw new NotImplementedException();
        public IPlotRepository GetPlotRepository() => throw new NotImplementedException();
        public IForumRepository GetForumRepository() => throw new NotImplementedException();
        public ICharacterRepository GetCharactersRepository() => throw new NotImplementedException();
        public IAccommodationRepository GetAccomodationRepository() => throw new NotImplementedException();
        public IKogdaIgraRepository GetKogdaIgraRepository() => throw new NotImplementedException();
        public IFinanceOperationsRepository GetFinanceOperationsRepositoryRepository() => throw new NotImplementedException();
    }

    private class StubProjectRepository : IProjectRepository
    {
        private readonly Project _project;
        public StubProjectRepository(Project project) => _project = project;

        public Task<Project> GetProjectAsync(int project) => Task.FromResult(_project);

        public Task<ProjectField> GetProjectField(int projectId, int projectCharacterFieldId)
            => Task.FromResult(_project.ProjectFields.Single(f => f.ProjectFieldId == projectCharacterFieldId && f.ProjectId == projectId));

        public void Dispose() { }
        public Task<Project> GetProjectWithDetailsAsync(int project) => throw new NotImplementedException();
        public Task<Project?> GetProjectWithFieldsAsync(int project) => throw new NotImplementedException();
        public Task<CharacterGroup?> GetGroupAsync(int projectId, int characterGroupId) => throw new NotImplementedException();
        public Task<CharacterGroup?> GetGroupAsync(CharacterGroupIdentification characterGroupId) => throw new NotImplementedException();
        public Task<CharacterGroup?> LoadGroupWithTreeAsync(int projectId, int? characterGroupId) => throw new NotImplementedException();
        public Task<CharacterGroup> LoadGroupWithTreeSlimAsync(int projectId) => throw new NotImplementedException();
        public Task<CharacterGroup> LoadGroupWithChildsAsync(int projectId, int characterGroupId) => throw new NotImplementedException();
        public Task<IList<CharacterGroup>> LoadGroups(int projectId, IReadOnlyCollection<int> groupIds) => throw new NotImplementedException();
        public Task<IList<CharacterGroup>> LoadGroups(IReadOnlyCollection<CharacterGroupIdentification> groupIds) => throw new NotImplementedException();
        public Task<ProjectField> GetProjectField(ProjectFieldIdentification id) => throw new NotImplementedException();
        public Task<ProjectFieldDropdownValue> GetFieldValue(int projectId, int projectFieldId, int projectCharacterFieldDropdownValueId) => throw new NotImplementedException();
        public Task<ProjectFieldDropdownValue> GetFieldValue(ProjectFieldIdentification id, int variantId) => throw new NotImplementedException();
        public Task<Project> GetProjectWithFinances(int projectid) => throw new NotImplementedException();
        public Task<Project> GetProjectForFinanceSetup(int projectid) => throw new NotImplementedException();
        public Task<ICollection<Character>> GetCharacterByGroups(IReadOnlyCollection<CharacterGroupIdentification> characterGroupIdentifications) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<ProjectWithUpdateDateDto>> GetStaleProjects(DateTime inActiveSince) => throw new NotImplementedException();
        public Task<ProjectPersonalizedInfo[]> GetPersonalizedProjectsBySpecification(PersonalizedProjectListSpecification projectListSpecification) => throw new NotImplementedException();
        public Task<ProjectShortInfo[]> GetProjectsBySpecification(ProjectListSpecification projectListSpecification) => throw new NotImplementedException();
        public Task<ProjectPersonalizedInfo[]> GetProjectsByIds(UserIdentification? userId, ProjectIdentification[] ids) => throw new NotImplementedException();
        public Task<CharacterGroupHeaderDto[]> LoadDirectChildGroupHeaders(CharacterGroupIdentification characterGroupId) => throw new NotImplementedException();
        public Task<CharacterGroupHeaderDto[]> GetGroupHeaders(IReadOnlyCollection<CharacterGroupIdentification> characterGroupIds) => throw new NotImplementedException();
    }

    private static MockedProject CreateProjectWithDropdownFields(int fieldCount, int variantsPerField)
    {
        var mock = new MockedProject();

        for (var i = 0; i < fieldCount; i++)
        {
            var fieldId = i + 1;
            var field = new ProjectField
            {
                Project = mock.Project,
                ProjectId = mock.Project.ProjectId,
                ProjectFieldId = fieldId,
                FieldName = $"Field {i}",
                FieldType = ProjectFieldType.Dropdown,
                FieldBoundTo = FieldBoundTo.Character,
                AvailableForCharacterGroupIds = [],
                IsActive = true,
                CharacterGroup = new CharacterGroup
                {
                    CharacterGroupId = -fieldId,
                    Project = mock.Project,
                    ProjectId = mock.Project.ProjectId,
                    ParentCharacterGroupIds = [mock.Project.RootGroup.CharacterGroupId],
                    CharacterGroupName = $"Field {i}",
                    IsActive = true,
                    IsSpecial = true,
                    IsRoot = false,
                },
            };
            mock.Project.CharacterGroups.Add(field.CharacterGroup);

            for (var j = 0; j < variantsPerField; j++)
            {
                var variantId = j + 1 + (i * 100);
                var variant = new ProjectFieldDropdownValue
                {
                    ProjectFieldDropdownValueId = variantId,
                    ProjectFieldId = field.ProjectFieldId,
                    ProjectId = field.ProjectId,
                    Project = field.Project,
                    ProjectField = field,
                    Label = $"Variant {i}-{j}",
                    IsActive = true,
                    PlayerSelectable = false,
                    Description = new MarkdownString(),
                    MasterDescription = new MarkdownString(),
                    CharacterGroup = new CharacterGroup
                    {
                        CharacterGroupId = -variantId,
                        Project = mock.Project,
                        ProjectId = mock.Project.ProjectId,
                        ParentCharacterGroupIds = [field.CharacterGroup.CharacterGroupId],
                        CharacterGroupName = $"Variant {i}-{j}",
                        IsActive = true,
                        IsSpecial = true,
                        IsRoot = false,
                    },
                };
                mock.Project.CharacterGroups.Add(variant.CharacterGroup);
                field.DropdownValues.Add(variant);
            }

            mock.Project.ProjectFields.Add(field);
        }

        return mock;
    }

    private static FieldSetupServiceImpl CreateService(Project project)
    {
        var repo = new StubProjectRepository(project);
        var uow = new StubUnitOfWork(repo);
        var currentUser = new StubCurrentUserAccessor();
        return new FieldSetupServiceImpl(uow, currentUser);
    }

    [Fact]
    public async Task MoveField_ShouldSyncSpecialGroupOrdering()
    {
        var mock = CreateProjectWithDropdownFields(fieldCount: 2, variantsPerField: 0);
        var field1 = mock.Project.ProjectFields.First();
        var field2 = mock.Project.ProjectFields.Last();

        mock.Project.RootGroup.ChildGroupsOrdering = $"{field1.CharacterGroup.CharacterGroupId},{field2.CharacterGroup.CharacterGroupId}";
        mock.Project.Details.FieldsOrdering = $"{field1.ProjectFieldId},{field2.ProjectFieldId}";

        var service = CreateService(mock.Project);

        await service.MoveField(mock.Project.ProjectId, field1.ProjectFieldId, direction: 1);

        var rootContainer = mock.Project.RootGroup.GetCharacterGroupsContainer();
        rootContainer.OrderedItems[0].CharacterGroupId.ShouldBe(field2.CharacterGroup.CharacterGroupId);
        rootContainer.OrderedItems[1].CharacterGroupId.ShouldBe(field1.CharacterGroup.CharacterGroupId);
    }

    [Fact]
    public async Task MoveFieldAfter_ShouldSyncSpecialGroupOrdering()
    {
        var mock = CreateProjectWithDropdownFields(fieldCount: 3, variantsPerField: 0);
        var fields = mock.Project.ProjectFields.ToList();
        var field1 = fields[0];
        var field2 = fields[1];
        var field3 = fields[2];

        mock.Project.RootGroup.ChildGroupsOrdering = $"{field1.CharacterGroup.CharacterGroupId},{field2.CharacterGroup.CharacterGroupId},{field3.CharacterGroup.CharacterGroupId}";
        mock.Project.Details.FieldsOrdering = $"{field1.ProjectFieldId},{field2.ProjectFieldId},{field3.ProjectFieldId}";

        var service = CreateService(mock.Project);

        await service.MoveFieldAfter(mock.Project.ProjectId, field3.ProjectFieldId, afterFieldId: field1.ProjectFieldId);

        var rootContainer = mock.Project.RootGroup.GetCharacterGroupsContainer();
        rootContainer.OrderedItems.Select(g => g.CharacterGroupId).ToArray()
            .ShouldBe([field1.CharacterGroup.CharacterGroupId, field3.CharacterGroup.CharacterGroupId, field2.CharacterGroup.CharacterGroupId]);
    }

    [Fact]
    public async Task MoveFieldVariant_ShouldSyncSpecialGroupOrdering()
    {
        var mock = CreateProjectWithDropdownFields(fieldCount: 1, variantsPerField: 2);
        var field = mock.Project.ProjectFields.Single();
        var variants = field.DropdownValues.ToList();
        var variant1 = variants[0];
        var variant2 = variants[1];

        field.ValuesOrdering = $"{variant1.ProjectFieldDropdownValueId},{variant2.ProjectFieldDropdownValueId}";
        field.CharacterGroup!.ChildGroupsOrdering = $"{variant1.CharacterGroup.CharacterGroupId},{variant2.CharacterGroup.CharacterGroupId}";

        var service = CreateService(mock.Project);

        await service.MoveFieldVariant(mock.Project.ProjectId, field.ProjectFieldId, variant1.ProjectFieldDropdownValueId, direction: 1);

        var groupContainer = field.CharacterGroup.GetCharacterGroupsContainer();
        groupContainer.OrderedItems[0].CharacterGroupId.ShouldBe(variant2.CharacterGroup.CharacterGroupId);
        groupContainer.OrderedItems[1].CharacterGroupId.ShouldBe(variant1.CharacterGroup.CharacterGroupId);
    }

    [Fact]
    public async Task SortFieldVariants_ShouldSyncSpecialGroupOrdering()
    {
        var mock = CreateProjectWithDropdownFields(fieldCount: 1, variantsPerField: 3);
        var field = mock.Project.ProjectFields.Single();
        var variants = field.DropdownValues.ToList();

        // Intentionally reverse labels so sorting changes order
        variants[0].Label = "Zebra";
        variants[1].Label = "Alpha";
        variants[2].Label = "Beta";
        variants[0].CharacterGroup.CharacterGroupName = "Zebra";
        variants[1].CharacterGroup.CharacterGroupName = "Alpha";
        variants[2].CharacterGroup.CharacterGroupName = "Beta";

        field.ValuesOrdering = $"{variants[0].ProjectFieldDropdownValueId},{variants[1].ProjectFieldDropdownValueId},{variants[2].ProjectFieldDropdownValueId}";
        field.CharacterGroup!.ChildGroupsOrdering = $"{variants[0].CharacterGroup.CharacterGroupId},{variants[1].CharacterGroup.CharacterGroupId},{variants[2].CharacterGroup.CharacterGroupId}";

        var service = CreateService(mock.Project);

        await service.SortFieldVariants(mock.Project.ProjectId, field.ProjectFieldId);

        var groupContainer = field.CharacterGroup.GetCharacterGroupsContainer();
        groupContainer.OrderedItems.Select(g => g.CharacterGroupId).ToArray()
            .ShouldBe([variants[1].CharacterGroup.CharacterGroupId, variants[2].CharacterGroup.CharacterGroupId, variants[0].CharacterGroup.CharacterGroupId]);
    }

    [Fact]
    public async Task MoveField_FieldWithoutSpecialGroup_ShouldNotFail()
    {
        var mock = new MockedProject();
        var field1 = new ProjectField
        {
            Project = mock.Project,
            ProjectId = mock.Project.ProjectId,
            ProjectFieldId = 1,
            FieldName = "String Field 1",
            FieldType = ProjectFieldType.String,
            FieldBoundTo = FieldBoundTo.Claim,
            AvailableForCharacterGroupIds = [],
            IsActive = true,
        };
        var field2 = new ProjectField
        {
            Project = mock.Project,
            ProjectId = mock.Project.ProjectId,
            ProjectFieldId = 2,
            FieldName = "String Field 2",
            FieldType = ProjectFieldType.String,
            FieldBoundTo = FieldBoundTo.Claim,
            AvailableForCharacterGroupIds = [],
            IsActive = true,
        };
        mock.Project.ProjectFields.Add(field1);
        mock.Project.ProjectFields.Add(field2);
        mock.Project.Details.FieldsOrdering = $"{field1.ProjectFieldId},{field2.ProjectFieldId}";

        var service = CreateService(mock.Project);

        await service.MoveField(mock.Project.ProjectId, field1.ProjectFieldId, direction: 1).ShouldNotThrowAsync();
    }
}
