using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using JoinRpg.Dal.Impl;
using JoinRpg.Dal.Impl.Migrations;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.DomainTypes.Users;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Виртуальные пользователи из сида миграций создаются в обход регистрации, но аватарка нужна и им (ADR026).
/// Нужна БД после сида, поэтому тест интеграционный.
/// </summary>
public class VirtualUsersSeedAvatarScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private static readonly string[] VirtualUsers = [User.RobotVirtualUser, User.OnlinePaymentVirtualUser];

    [Fact]
    public async Task AfterMigrations_EveryUserHasSelectedAvatar()
    {
        // Без прогона DefaultAvatarsBackfillJob: фабрика убирает фоновые сервисы, а в свежей базе
        // есть только пользователи из сида.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        foreach (var userName in VirtualUsers)
        {
            var user = await LoadUser(db, userName);
            user.SelectedAvatar.ShouldNotBeNull().AvatarSource.ShouldBe(AvatarSource.GrAvatar);
            user.Avatars.Count.ShouldBe(1);
        }
        (await db.Set<User>().AnyAsync(u => u.SelectedAvatarId == null)).ShouldBeFalse();
    }

    [Fact]
    public async Task RepeatedMigrations_DoNotAddAvatars()
    {
        RunMigrations();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        foreach (var userName in VirtualUsers)
        {
            (await LoadUser(db, userName)).Avatars.Count.ShouldBe(1);
        }
    }

    /// <summary>
    /// В базах, где сид уже отработал старой версией, виртуальные пользователи остались без аватарки.
    /// Повторный прогон миграций должен её завести.
    /// </summary>
    /// <remarks>
    /// База у тестов класса общая: тест сам возвращает аватарку прогоном миграций. Если он упадёт до прогона,
    /// следом упадут и проверки остальных тестов — причину искать здесь.
    /// </remarks>
    [Fact]
    public async Task Seed_RestoresMissingAvatarForRobot()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var robot = await LoadUser(db, User.RobotVirtualUser);
            robot.SelectedAvatar = null!;
            db.Set<UserAvatar>().RemoveRange(robot.Avatars.ToList());
            _ = await db.SaveChangesAsync();
        }

        RunMigrations();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var robot = await LoadUser(db, User.RobotVirtualUser);
            robot.SelectedAvatar.ShouldNotBeNull().AvatarSource.ShouldBe(AvatarSource.GrAvatar);
            robot.Avatars.Count.ShouldBe(1);
        }
    }

    private void RunMigrations()
    {
        var migConfig = new Configuration();
        migConfig.TargetDatabase = new DbConnectionInfo(factory.ConnectionString, "System.Data.SqlClient");
        new DbMigrator(migConfig).Update();
    }

    private static Task<User> LoadUser(MyDbContext db, string userName)
        => db.Set<User>()
            .Include(u => u.Avatars)
            .Include(u => u.SelectedAvatar)
            .SingleAsync(u => u.UserName == userName);
}
