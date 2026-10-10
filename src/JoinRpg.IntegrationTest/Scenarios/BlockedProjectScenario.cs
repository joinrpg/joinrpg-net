using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.ProjectMetadata;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectAccess;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Заблокированный проект (ADR023): обычные изменения запрещены, фоновые джобы его не трогают,
/// а выдача прав мастерам остаётся доступной — чтобы было кому восстанавливать проект.
/// </summary>
/// <remarks>
/// Сервиса, ставящего блокировку, пока нет, поэтому флаг <c>IsBlocked</c> ставим прямо в БД.
/// Метаданные проекта кешируются на скоуп, поэтому после блокировки всё делаем в новом скоупе.
/// </remarks>
public class BlockedProjectScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task Metadata_ReportsBlockedStatus()
    {
        var (masterId, projectId) = await CreateMasterAndProject();
        await BlockProject(projectId);

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            projectInfo.ProjectStatus.ShouldBe(ProjectLifecycleStatus.Blocked);
            projectInfo.IsActive.ShouldBeFalse();
            projectInfo.IsArchived.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task GetStaleProjects_ExcludesBlockedProject()
    {
        var (_, projectId) = await CreateMasterAndProject();

        // Порог в будущем — свежий активный проект попадает в «заброшенные».
        (await GetStaleProjectIds()).ShouldContain(projectId.Value);

        await BlockProject(projectId);

        // Заблокированный проект джоба закрытия не трогает.
        (await GetStaleProjectIds()).ShouldNotContain(projectId.Value);
    }

    [Fact]
    public async Task EnsureRolesListsJob_SkipsBlockedProject()
    {
        var (masterId, projectId) = await CreateMasterAndProject();

        // Воспроизводим «старый» проект без сеток ролей и блокируем его.
        await RemoveAllRolesLists(projectId);
        await BlockProject(projectId);

        await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<EnsureRolesListsJob>().RunOnce(CancellationToken.None));

        // Джоба не добавила сеток в заблокированный проект.
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var lists = await sp.GetRequiredService<IProjectRolesListRepository>().GetForProjectAsync(projectId);
            lists.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task Forum_CreateThreadAndAddComment_ThrowInBlockedProject()
    {
        var (masterId, projectId) = await CreateMasterAndProject();

        // Тему заводим до блокировки, чтобы проверить и добавление комментария в неё.
        var threadId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IForumService>().CreateThread(
                projectInfo.GroupTree.RootGroupId,
                "Тема до блокировки",
                "Первый комментарий в теме",
                hideFromUser: false,
                emailEverybody: false);
        });

        await BlockProject(projectId);

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            _ = await Should.ThrowAsync<ProjectBlockedException>(() =>
                sp.GetRequiredService<IForumService>().CreateThread(
                    projectInfo.GroupTree.RootGroupId,
                    "Тема после блокировки",
                    "Не должна появиться",
                    hideFromUser: false,
                    emailEverybody: false));
        });

        await factory.Services.RunAsAsync(masterId, async sp =>
            _ = await Should.ThrowAsync<ProjectBlockedException>(() =>
                sp.GetRequiredService<IForumService>().AddComment(
                    threadId,
                    parentCommentId: null,
                    isVisibleToPlayer: true,
                    "Комментарий после блокировки")));
    }

    [Fact]
    public async Task AddCharacter_Throws_ButGrantAccess_SucceedsInBlockedProject()
    {
        var (masterId, projectId) = await CreateMasterAndProject();
        UserIdentification newMasterId;
        using (var scope = factory.Services.CreateScope())
        {
            newMasterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }

        await BlockProject(projectId);

        // Обычное изменение проекта запрещено.
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            _ = await Should.ThrowAsync<ProjectBlockedException>(() =>
                sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                    projectId,
                    ParentCharacterGroupIds: [],
                    new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                    FieldValues: FieldLayerContainer.Empty(projectInfo))));
        });

        // А добавить мастера можно — чтобы было кому восстанавливать проект.
        await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = newMasterId,
                Role = new("Восстановитель"),
                Description = new MarkdownString("Помогает восстановить проект"),
                IsPublic = false,
                Permissions = [Permission.CanManageClaims],
            }));

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            projectInfo.HasMasterAccess(newMasterId, Permission.CanManageClaims).ShouldBeTrue();
        });
    }

    private async Task<(UserIdentification masterId, ProjectIdentification projectId)> CreateMasterAndProject()
    {
        using var scope = factory.Services.CreateScope();
        var masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(
            scope.ServiceProvider, masterId, "Проект для блокировки");
        return (masterId, projectId);
    }

    private async Task<int[]> GetStaleProjectIds()
    {
        using var scope = factory.Services.CreateScope();
        var stale = await scope.ServiceProvider.GetRequiredService<IProjectRepository>()
            .GetStaleProjects(DateTime.UtcNow.AddDays(1));
        return [.. stale.Select(p => p.ProjectId)];
    }

    /// <summary>
    /// Сервиса блокировки пока нет — ставим флаг прямо в БД.
    /// </summary>
    private async Task BlockProject(ProjectIdentification projectId)
    {
        using var scope = factory.Services.CreateScope();
        var myDb = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        _ = await myDb.Database.ExecuteSqlCommandAsync(
            "UPDATE dbo.Projects SET IsBlocked = 1 WHERE ProjectId = @p0",
            projectId.Value);
    }

    /// <summary>
    /// Сетку по умолчанию сервис удалить не даёт, поэтому авто-сетки сносим прямо в БД.
    /// </summary>
    private async Task RemoveAllRolesLists(ProjectIdentification projectId)
    {
        using var scope = factory.Services.CreateScope();
        var myDb = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        _ = await myDb.Database.ExecuteSqlCommandAsync(
            """
            UPDATE dbo.ProjectDetails SET DefaultProjectRolesListId = NULL WHERE ProjectId = @p0;
            DELETE FROM dbo.ProjectRolesLists WHERE ProjectId = @p0;
            """,
            projectId.Value);
    }
}
