using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Регистрация с приёмом взноса на стойке (#4890): приём денег и регистрация — две операции, и
/// регистрация видит уже принятый взнос.
/// </summary>
/// <remarks>
/// Пока это была одна операция, долг проверялся по снимку заявки, снятому до приёма денег, и
/// регистрация с приёмом взноса не проходила никогда.
/// </remarks>
public class CheckInFeeAtDeskScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const string Password = "Password123!";
    private const int Fee = 1000;

    [Fact]
    public async Task FeeAcceptedAtDesk_PlayerIsCheckedIn()
    {
        var (masterId, client, projectId) = await CreateProjectAsync(passportRequired: false);
        var claimId = await SeedApprovedClaimWithFeeAsync(masterId, projectId);

        var response = await PostCheckInAsync(client, claimId, money: Fee);

        response.StatusCode.ShouldBe(HttpStatusCode.Found, await response.DescribeValidationErrorsAsync());
        var claim = await LoadClaimAsync(masterId, claimId);
        claim.Claim.Status.ShouldBe(ClaimStatus.CheckedIn);
        claim.Claim.Finance.FeePaid.ShouldBe(Fee);
    }

    /// <summary>
    /// Деньги приняты, а зарегистрировать нельзя (не хватает паспорта): взнос остаётся принятым, форма
    /// показывает это, а повторная отправка той же формы второй раз деньги не принимает.
    /// </summary>
    [Fact]
    public async Task CheckInFailsAfterFeeAccepted_FeeIsKeptAndNotTakenTwice()
    {
        var (masterId, client, projectId) = await CreateProjectAsync(passportRequired: true);
        var claimId = await SeedApprovedClaimWithFeeAsync(masterId, projectId);

        var response = await PostCheckInAsync(client, claimId, money: Fee);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.AsHtmlDocument();
        WebUtility.HtmlDecode(page.DocumentNode.InnerText).ShouldNotContain("Принять взнос");
        var claim = await LoadClaimAsync(masterId, claimId);
        claim.Claim.Status.ShouldBe(ClaimStatus.Approved);
        claim.Claim.Finance.FeePaid.ShouldBe(Fee);

        // Та же форма ещё раз — например, мастер нажал «назад» и отправил снова.
        _ = await PostCheckInAsync(client, claimId, money: Fee);

        (await LoadClaimAsync(masterId, claimId)).Claim.Finance.FeePaid.ShouldBe(Fee);
    }

    private async Task<HttpResponseMessage> PostCheckInAsync(HttpClient client, ClaimIdentification claimId, int money)
    {
        var token = await client.GetAntiforgeryTokenAsync($"{claimId.ProjectId.Value}/claim/{claimId.ClaimId}/checkin");
        return await client.PostFormAsync(
            $"{claimId.ProjectId.Value}/checkin/DoCheckIn",
            token,
            ("projectId", claimId.ProjectId.Value.ToString()),
            ("claimId", claimId.ClaimId.ToString()),
            ("money", money.ToString()),
            ("feeAccepted", "on"));
    }

    private Task<ClaimInfo> LoadClaimAsync(UserIdentification masterId, ClaimIdentification claimId)
        => factory.Services.RunAsAsync(masterId, async sp =>
            (await sp.GetRequiredService<IClaimInfoRepository>().GetClaimInfoOrDefault(claimId)).ShouldNotBeNull());

    private async Task<(UserIdentification MasterId, HttpClient Client, ProjectIdentification ProjectId)> CreateProjectAsync(bool passportRequired)
    {
        var (masterId, masterEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(factory.Services, password: Password);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(factory.Services, masterId, "Регистрация со взносом на стойке");

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await projectService.SetClaimSettings(projectId, projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
            await projectService.SetCheckInSettings(projectId, checkInProgress: true, enableCheckInModule: true, modelAllowSecondRoles: false);
            if (passportRequired)
            {
                await projectService.SetContactSettings(projectId,
                    ProjectProfileRequirementSettings.AllNotRequired with { RequirePassport = MandatoryStatus.Required });
            }

            await sp.GetRequiredService<IProjectFinanceSettingsService>().CreatePaymentType(new CreatePaymentTypeRequest
            {
                ProjectId = projectId,
                TargetMasterId = masterId,
                TypeKind = PaymentTypeKind.Cash,
                Name = null,
            });
        });

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            masterEmail,
            Password,
            followsRedirects: false);

        return (masterId, client, projectId);
    }

    private async Task<ClaimIdentification> SeedApprovedClaimWithFeeAsync(UserIdentification masterId, ProjectIdentification projectId)
    {
        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [projectInfo.GroupTree.RootGroupId],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        var (playerId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(factory.Services);
        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Заявка от игрока", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: true);
        });

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            await sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято");
            await sp.GetRequiredService<IFinanceService>().ChangeFee(claimId, Fee);
        });
        return claimId;
    }
}
