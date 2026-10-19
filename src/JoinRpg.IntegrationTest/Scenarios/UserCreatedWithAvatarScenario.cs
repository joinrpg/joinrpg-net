using System.Data.Entity;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Users;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Пользователь получает аватарку Gravatar в момент создания (#5602), а не на первом GET-запросе.
/// </summary>
public class UserCreatedWithAvatarScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task NewUser_HasSelectedGravatar()
    {
        using var scope = factory.Services.CreateScope();
        var userId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);

        using var checkScope = factory.Services.CreateScope();
        var db = checkScope.ServiceProvider.GetRequiredService<MyDbContext>();
        var user = await db.Set<User>()
            .Include(u => u.SelectedAvatar)
            .Include(u => u.Avatars)
            .SingleAsync(u => u.UserId == userId.Value);

        user.SelectedAvatar.ShouldNotBeNull();
        user.SelectedAvatar.AvatarSource.ShouldBe(AvatarSource.GrAvatar);
        user.SelectedAvatar.OriginalUri.ShouldStartWith("https://www.gravatar.com/avatar/");
        user.Avatars.Count.ShouldBe(1);
    }
}
