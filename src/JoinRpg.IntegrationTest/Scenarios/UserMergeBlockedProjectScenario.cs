using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Users;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Пока в базе есть заблокированный проект (идёт восстановление, ADR023), сливать нельзя никого
/// (ADR024): восстановление может вставить строки со ссылкой на уже удалённого поглощаемого.
/// </summary>
/// <remarks>
/// Отдельный класс — отдельная база: блокировка любого проекта запрещает слияние всем, и в общем
/// с другими сценариями классе ломала бы их. Блокировка снимается в finally по той же причине.
/// </remarks>
public class UserMergeBlockedProjectScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task AnyBlockedProject_BlocksMerge_EvenUnrelated()
    {
        var a = await CreateUser();
        var b = await CreateUser();
        var projectId = await CreateProject(await CreateUser()); // ни A, ни B там нет

        await WithBlocked(projectId, async () =>
        {
            var preview = await GetPreview(a, b);
            preview.Conflicts.ShouldBe([new UserMergeConflict.BlockedProject(projectId)]);

            _ = await Should.ThrowAsync<UserMergeConflictException>(
                factory.Services.RunAsAsync(a, sp => sp.GetRequiredService<IUserMergeService>().Merge(a, b), isAdmin: true));
            using var scope = factory.Services.CreateScope();
            (await scope.ServiceProvider.GetRequiredService<IUserRepository>().GetUserInfo(b)).ShouldNotBeNull();
        });

        (await GetPreview(a, b)).CanMerge.ShouldBeTrue();
    }

    [Fact]
    public async Task ArchivedProject_DoesNotBlock()
    {
        var a = await CreateUser();
        var b = await CreateUser();
        var projectId = await CreateProject(b);
        await SetProjectFlags(projectId, "[Active] = 0");

        (await GetPreview(a, b)).CanMerge.ShouldBeTrue();
    }

    private async Task WithBlocked(ProjectIdentification projectId, Func<Task> action)
    {
        await SetProjectFlags(projectId, "[IsBlocked] = 1");
        try
        {
            await action();
        }
        finally
        {
            await SetProjectFlags(projectId, "[IsBlocked] = 0");
        }
    }

    private async Task SetProjectFlags(ProjectIdentification projectId, string assignment)
    {
        using var scope = factory.Services.CreateScope();
        _ = await scope.ServiceProvider.GetRequiredService<MyDbContext>().Database
            .ExecuteSqlCommandAsync($"UPDATE [Projects] SET {assignment} WHERE [ProjectId] = @p0", projectId.Value);
    }

    private Task<UserMergePreview> GetPreview(UserIdentification a, UserIdentification b)
        => factory.Services.RunAsAsync(a, sp => sp.GetRequiredService<IUserMergeService>().GetPreview(a, b), isAdmin: true);

    private async Task<UserIdentification> CreateUser()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
    }

    private async Task<ProjectIdentification> CreateProject(UserIdentification ownerId)
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);
    }
}
