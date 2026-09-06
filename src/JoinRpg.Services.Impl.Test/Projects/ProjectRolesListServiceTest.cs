using JoinRpg.DataModel;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Projects.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Projects;

/// <summary>
/// Тесты <see cref="ProjectRolesListService"/>: переключение и защита сетки ролей по умолчанию.
/// </summary>
public class ProjectRolesListServiceTest : ProjectMetadataServiceTestBase
{
    private ProjectRolesListService CreateService(int? currentUserId = null, bool isAdmin = false)
    {
        var currentUser = CreateCurrentUser(currentUserId, isAdmin);
        return new ProjectRolesListService(
            CreatePropsService(currentUser),
            metadataRepository,
            currentUser,
            NullLogger<ProjectRolesListService>.Instance);
    }

    /// <summary>Добавляет сохранённую сетку ролей напрямую в мок (минуя сервис создания).</summary>
    private ProjectRolesListIdentification AddRolesList(string name)
    {
        var id = mock.Project.ProjectRolesLists.Count == 0 ? 1 : mock.Project.ProjectRolesLists.Max(x => x.ProjectRolesListId) + 1;
        mock.Project.ProjectRolesLists.Add(new DataModel.ProjectRolesList
        {
            ProjectRolesListId = id,
            ProjectId = mock.Project.ProjectId,
            Project = mock.Project,
            Name = name,
            FieldsImpl = new IntList(),
        });
        mock.ReInitProjectInfo();
        return new ProjectRolesListIdentification(ProjectId, id);
    }

    [Fact]
    public async Task SetDefaultAsync_MakesRolesListDefault()
    {
        _ = AddRolesList("Первая");
        var secondId = AddRolesList("Вторая");

        await CreateService().SetDefaultAsync(secondId);

        Result.DefaultRolesListId.ShouldBe(secondId);
        mock.Project.Details.DefaultProjectRolesListId.ShouldBe(secondId.ProjectRolesListId);
    }

    [Fact]
    public async Task RemoveAsync_OnDefaultRolesList_Throws()
    {
        var id = AddRolesList("Единственная");
        await CreateService().SetDefaultAsync(id);

        await Should.ThrowAsync<InvalidOperationException>(() => CreateService().RemoveAsync(id));

        mock.Project.ProjectRolesLists.ShouldContain(x => x.ProjectRolesListId == id.ProjectRolesListId);
    }

    [Fact]
    public async Task RemoveAsync_OnNonDefaultRolesList_Removes()
    {
        var defaultId = AddRolesList("По умолчанию");
        var otherId = AddRolesList("Другая");
        await CreateService().SetDefaultAsync(defaultId);

        await CreateService().RemoveAsync(otherId);

        mock.Project.ProjectRolesLists.ShouldNotContain(x => x.ProjectRolesListId == otherId.ProjectRolesListId);
    }

    /// <summary>
    /// Проект может сразу получить несколько сеток ролей (см. CreateProjectService.SetupLarp) —
    /// сеткой по умолчанию должна стать первая из них, а не последняя.
    /// </summary>
    [Fact]
    public async Task CreateAsync_FirstRolesList_BecomesDefault()
    {
        await CreateService().CreateAsync(new DomainTypes.ProjectMetadata.ProjectRolesList(ProjectId, "Первая", [], RolesGridGroupsViewMode.None));
        await CreateService().CreateAsync(new DomainTypes.ProjectMetadata.ProjectRolesList(ProjectId, "Вторая", [], RolesGridGroupsViewMode.None));

        mock.Project.Details.DefaultProjectRolesList.ShouldNotBeNull().Name.ShouldBe("Первая");
    }
}
