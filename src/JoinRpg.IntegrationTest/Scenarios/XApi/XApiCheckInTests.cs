using System.Net;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiCheckInTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        await Should.ThrowAsync<HttpRequestException>(
            () => fixture.AnonymousXApiClient.GetCheckInClaimsAsync(fixture.ProjectId));
    }

    [Fact]
    public async Task MasterCanGetEmptyCheckInList()
    {
        var claims = await fixture.MasterClient.GetCheckInClaimsAsync(fixture.ProjectId);
        claims.ShouldBeEmpty();
    }

    [Fact]
    public async Task MasterCanGetCheckInStats()
    {
        var stats = await fixture.MasterClient.GetCheckInStatsAsync(fixture.ProjectId);
        stats.CheckIn.ShouldBe(0);
        stats.Ready.ShouldBe(0);
    }

    [Fact]
    public async Task PrepareNonexistentClaim_Returns404()
    {
        var status = await fixture.MasterClient.PrepareClaimForCheckInRawAsync(fixture.ProjectId.Value, claimId: 999999);
        status.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Заявка другого проекта того же мастера не находится по чужому projectId. Проверку проекта
    /// держит загрузчик агрегата; раньше её держал отдельный запрос EF-сущности заявки.
    /// </summary>
    [Fact]
    public async Task PrepareClaimOfAnotherProject_Returns404()
    {
        var otherProjectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(otherProjectId, new CreateCharacterRequest());
        await fixture.OpenClaims(otherProjectId);

        var (playerId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(fixture.Factory.Services);
        var claimId = await fixture.AddClaimFromPlayer(new CharacterIdentification(otherProjectId, character.CharacterId), playerId);

        var status = await fixture.MasterClient.PrepareClaimForCheckInRawAsync(fixture.ProjectId.Value, claimId.ClaimId);

        status.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Заявка утверждена, взносов и обязательных полей нет — зарегистрировать её можно. Заодно
    /// проверяет, что заявка, персонаж и профиль игрока загружаются согласованными: иначе
    /// конструктор <c>ClaimInfo</c> бросил бы, и ответ был бы 500.
    /// </summary>
    [Fact]
    public async Task MasterCanPrepareApprovedClaimForCheckIn()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        await fixture.OpenClaims(projectId);

        var (playerId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(fixture.Factory.Services);
        var claimId = await fixture.AddClaimFromPlayer(new CharacterIdentification(projectId, character.CharacterId), playerId);
        await fixture.ApproveClaim(claimId);

        var result = await fixture.MasterClient.PrepareClaimForCheckInAsync(projectId, claimId.ClaimId);

        result.ClaimId.ShouldBe(claimId.ClaimId);
        result.Approved.ShouldBeTrue();
        result.CheckedIn.ShouldBeFalse();
        result.CheckInPossible.ShouldBeTrue();
        result.ClaimFeeBalance.ShouldBe(0);
    }
}
