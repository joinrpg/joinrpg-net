using System.Net;
using JoinRpg.Dal.Impl;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

/// <summary>
/// Ручка GET x-api/users/{userId}/ — доступна только сайт-администраторам ([XAdminAuthorize]),
/// раньше не имела ни одного интеграционного теста.
/// </summary>
[Collection("XApi")]
public class XApiUserInfoTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        await Should.ThrowAsync<HttpRequestException>(
            () => fixture.AnonymousXApiClient.GetUserInfoAsync(fixture.MasterUserId.Value));
    }

    [Fact]
    public async Task MasterIsNotSiteAdmin_Returns403()
    {
        // MasterClient — обычный мастер проекта (роль в рамках проекта), а не сайт-администратор,
        // поэтому [XAdminAuthorize] должен его отклонить.
        var statusCode = await fixture.MasterClient.GetUserInfoRawAsync(fixture.MasterUserId.Value);

        statusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_ReturnsPlayerInfo()
    {
        var (playerId, playerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(fixture.Factory.Services);
        var adminClient = await CreateSiteAdminClientAsync();

        var result = await adminClient.GetUserInfoAsync(playerId.Value);

        result.PlayerId.ShouldBe(playerId.Value);
        result.PlayerContacts.Email.ShouldBe(playerEmail);
        result.NickName.ShouldNotBeNullOrEmpty();
        result.AvatarUrl.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task UnknownUser_Returns410()
    {
        var adminClient = await CreateSiteAdminClientAsync();

        var statusCode = await adminClient.GetUserInfoRawAsync(int.MaxValue);

        statusCode.ShouldBe(HttpStatusCode.Gone);
    }

    /// <summary>
    /// Создаёт пользователя, вручную выставляет ему флаг Auth.IsAdmin (сайт-администратор)
    /// и возвращает уже залогиненный под ним XApiClient.
    /// </summary>
    private async Task<XApiClient> CreateSiteAdminClientAsync()
    {
        const string password = "Password123!";
        var (_, adminEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(fixture.Factory.Services, password: password);

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var myDb = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var dbAdmin = myDb.Set<JoinRpg.DataModel.User>().Include("Auth").Single(u => u.Email == adminEmail);
            dbAdmin.Auth.IsAdmin = true;
            await myDb.SaveChangesAsync();
        }

        return await XApiClient.CreateXApiClient(fixture.Factory.CreateClient(), adminEmail, password);
    }
}
