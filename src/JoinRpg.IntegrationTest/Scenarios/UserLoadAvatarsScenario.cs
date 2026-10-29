using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Аватарки пачкой — <c>IUserRepository.LoadAvatars</c> (ADR026, #5774).
/// </summary>
/// <remarks>
/// Нужна БД: проверяется, что выборка по списку id (<c>Contains</c> в SQL) отдаёт ровно найденные
/// аватарки — без повторов и без отсутствующих.
/// </remarks>
public class UserLoadAvatarsScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task LoadAvatars_ReturnsFoundOnce_SkipsMissing()
    {
        var first = await CreateUserAvatarAsync();
        var second = await CreateUserAvatarAsync();
        var missing = new AvatarIdentification(int.MaxValue);

        var avatars = await LoadAvatarsAsync([first.Id, second.Id, first.Id, missing]);

        avatars.Select(a => a.UserAvatarId).ShouldBe([first.Id.Value, second.Id.Value], ignoreOrder: true);
        avatars.Single(a => a.UserAvatarId == first.Id.Value).OriginalUri.ShouldBe(first.OriginalUri);
    }

    [Fact]
    public async Task LoadAvatars_EmptyRequest_ReturnsEmpty()
    {
        var avatars = await LoadAvatarsAsync([]);

        avatars.ShouldBeEmpty();
    }

    private async Task<IReadOnlyCollection<UserAvatar>> LoadAvatarsAsync(IReadOnlyCollection<AvatarIdentification> ids)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IUserRepository>().LoadAvatars(ids);
    }

    private async Task<(AvatarIdentification Id, string OriginalUri)> CreateUserAvatarAsync()
    {
        using var scope = factory.Services.CreateScope();
        // Новый пользователь сразу получает аватарку Gravatar (#5602).
        var userId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);

        using var checkScope = factory.Services.CreateScope();
        var avatar = await checkScope.ServiceProvider.GetRequiredService<MyDbContext>()
            .Set<User>()
            .Where(u => u.UserId == userId.Value)
            .Select(u => u.SelectedAvatar)
            .SingleAsync();
        return (new AvatarIdentification(avatar.UserAvatarId), avatar.OriginalUri);
    }
}
