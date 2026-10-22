using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using JoinRpg.Dal.Impl;
using JoinRpg.Dal.Impl.Migrations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Полные <c>UserInfo</c> всех админов загружаются: так получателей добирает
/// <c>NotificationServiceImpl</c> для уведомлений «всем админам».
/// </summary>
/// <remarks>
/// Среди админов есть робот из сида миграций. Раньше сид создавал его без <c>UserExtra</c>,
/// проекция <c>GetUserInfosByPredicate</c> на нём падала, и до админов не доходило
/// ни одно уведомление (#5807). Нужна БД после сида, поэтому тест интеграционный.
/// </remarks>
public class AdminUserInfosScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task GetUserInfos_ForAllAdmins_IncludesRobot()
    {
        using var scope = factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var admins = await userRepository.GetAdminUserInfoHeaders();
        var userInfos = await userRepository.GetUserInfos([.. admins.Select(a => a.UserId)]);

        userInfos.Select(u => u.Email.Value).ShouldContain(User.RobotVirtualUser);
    }

    /// <summary>
    /// В базах, где сид уже отработал старой версией, робот остался без <c>UserExtra</c>.
    /// Повторный прогон миграций должен её досоздать.
    /// </summary>
    [Fact]
    public async Task Seed_RestoresMissingUserExtraForRobot()
    {
        var robotId = GetRobotId();
        await ExecuteAsync(db => db.Set<UserExtra>().Remove(db.Set<UserExtra>().Single(e => e.UserId == robotId)));

        var migConfig = new Configuration();
        migConfig.TargetDatabase = new DbConnectionInfo(factory.ConnectionString, "System.Data.SqlClient");
        new DbMigrator(migConfig).Update();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        db.Set<UserExtra>().Any(e => e.UserId == robotId).ShouldBeTrue();
    }

    private int GetRobotId()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        return db.Set<User>().Where(u => u.UserName == User.RobotVirtualUser).Select(u => u.UserId).Single();
    }

    private async Task ExecuteAsync(Action<MyDbContext> action)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        action(db);
        _ = await db.SaveChangesAsync();
    }
}
