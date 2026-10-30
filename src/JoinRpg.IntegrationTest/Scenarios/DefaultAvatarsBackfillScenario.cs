using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebInfrastructure.Job;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.DomainTypes.Users;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Бэкфилл аватарок по умолчанию (#5602) на настоящей базе.
/// </summary>
public class DefaultAvatarsBackfillScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task UsersWithoutSelectedAvatar_GetGravatar()
    {
        UserIdentification withoutAvatars, withUnselectedGravatar, withSelected;
        using (var scope = factory.Services.CreateScope())
        {
            withoutAvatars = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            withUnselectedGravatar = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            withSelected = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }

        int unselectedGravatarId, selectedId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            // Как у старого пользователя: ни одной аватарки.
            var old = await LoadUser(db, withoutAvatars);
            old.SelectedAvatar = null;
            db.Set<UserAvatar>().RemoveRange(old.Avatars.ToList());

            // Gravatar есть, но не выбран — его и надо выбрать, а не заводить второй.
            var unselected = await LoadUser(db, withUnselectedGravatar);
            unselectedGravatarId = unselected.SelectedAvatarId!.Value;
            unselected.SelectedAvatar = null;

            selectedId = (await LoadUser(db, withSelected)).SelectedAvatarId!.Value;

            _ = await db.SaveChangesAsync();
        }

        await RunJob();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            var old = await LoadUser(db, withoutAvatars);
            old.SelectedAvatar.ShouldNotBeNull().AvatarSource.ShouldBe(AvatarSource.GrAvatar);
            old.Avatars.Count.ShouldBe(1);

            var unselected = await LoadUser(db, withUnselectedGravatar);
            unselected.SelectedAvatarId.ShouldBe(unselectedGravatarId);
            unselected.Avatars.Count.ShouldBe(1);

            (await LoadUser(db, withSelected)).SelectedAvatarId.ShouldBe(selectedId);

            (await db.Set<User>().AnyAsync(u => u.SelectedAvatarId == null)).ShouldBeFalse();
        }

        // Идемпотентность: второй прогон ничего не находит и ничего не добавляет.
        await RunJob();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            (await LoadUser(db, withoutAvatars)).Avatars.Count.ShouldBe(1);
        }
    }

    private static Task<User> LoadUser(MyDbContext db, UserIdentification userId)
        => db.Set<User>()
            .Include(u => u.Avatars)
            .Include(u => u.SelectedAvatar)
            .SingleAsync(u => u.UserId == userId.Value);

    private async Task RunJob()
    {
        using var scope = factory.Services.CreateScope();
        var runner = scope.ServiceProvider.GetServices<IJobRunner>().Single(r => r.Name == "DefaultAvatarsBackfillJob");
        await runner.RunJob(scope, CancellationToken.None);
    }
}
