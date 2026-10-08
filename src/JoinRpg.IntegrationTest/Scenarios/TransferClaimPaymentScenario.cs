using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Мастер переводит взнос с одной заявки на другую через форму страницы перевода.
/// </summary>
/// <remarks>
/// Заявку-получателя на странице выбирает <c>ClaimSelector</c>, а он постит полный
/// идентификатор заявки, а не число. Тест проверяет, что форма и ручка друг друга понимают:
/// значение берётся из отрисованной страницы и уходит обратно как есть.
/// </remarks>
public class TransferClaimPaymentScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const int Paid = 300;
    private const int Transferred = 100;

    [Fact]
    public async Task MasterTransfersPayment_ToClaimPickedInSelector()
    {
        UserIdentification masterId;
        string masterEmail;
        ProjectIdentification projectId;
        UserIdentification firstPlayerId;
        UserIdentification secondPlayerId;
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, masterEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для перевода взноса");
            firstPlayerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            secondPlayerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }

        var (firstCharacterId, secondCharacterId) = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            await sp.GetRequiredService<IProjectFinanceSettingsService>().CreatePaymentType(new CreatePaymentTypeRequest
            {
                ProjectId = projectId,
                TargetMasterId = masterId,
                TypeKind = PaymentTypeKind.Cash,
                Name = null,
            });

            var characterService = sp.GetRequiredService<ICharacterService>();
            var first = await characterService.AddCharacter(NewCharacter(projectId, projectInfo));
            var second = await characterService.AddCharacter(NewCharacter(projectId, projectInfo));
            return (first, second);
        });

        var fromClaimId = await AddClaimAsync(firstPlayerId, projectId, firstCharacterId);
        var toClaimId = await AddClaimAsync(secondPlayerId, projectId, secondCharacterId);

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            await claimService.ApproveByMaster(fromClaimId, "Принято");
            await claimService.ApproveByMaster(toClaimId, "Принято");

            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IFinanceService>().FeeAcceptedOperation(new FeeAcceptedOperationRequest
            {
                ClaimId = fromClaimId.ClaimId,
                Contents = "Взнос",
                OperationDate = DateTime.UtcNow.Date,
                Money = Paid,
                PaymentTypeId = projectInfo.ProjectFinanceSettings.GetCashPaymentType(masterId)!.PaymentTypeId,
            });
        });

        // Успех — 302 на заявку; переход по нему замерил бы уже другой маршрут.
        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            masterEmail,
            followsRedirects: false);

        var pageUrl = $"{projectId.Value}/claim/{fromClaimId.ClaimId}/TransferClaimPayment";
        var page = await client.GetAsync(pageUrl);
        page.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await page.AsHtmlDocument();

        var options = html.DocumentNode
            .SelectNodes("//select[@name='RecipientClaimId']/option")
            .ShouldNotBeNull()
            .Select(o => o.GetAttributeValue("value", ""))
            .ToArray();
        // Заявку-источник получателем не предлагают.
        options.ShouldBe([toClaimId.ToString()]);

        var token = html.DocumentNode
            .SelectSingleNode($"//input[@name='{AntiforgeryPostHelpers.FormFieldName}']")
            .ShouldNotBeNull()
            .GetAttributeValue("value", "");

        var response = await client.PostFormAsync(
            pageUrl,
            token,
            ("ProjectId", projectId.Value.ToString()),
            ("ClaimId", fromClaimId.ClaimId.ToString()),
            ("CommentDiscussionId", fromClaimId.ClaimId.ToString()),
            ("OperationDate", DateTime.UtcNow.Date.ToString("yyyy-MM-dd")),
            ("RecipientClaimId", options[0]),
            ("Money", Transferred.ToString()),
            ("CommentText", "Перезачёт взноса"));

        response.StatusCode.ShouldBe(HttpStatusCode.Found, await response.DescribeValidationErrorsAsync());

        using var checkScope = factory.Services.CreateScope();
        var received = checkScope.ServiceProvider.GetRequiredService<MyDbContext>().Set<FinanceOperation>()
            .Single(fo => fo.ProjectId == projectId.Value && fo.ClaimId == toClaimId.ClaimId);
        received.OperationType.ShouldBe(FinanceOperationType.TransferFrom);
        received.MoneyAmount.ShouldBe(Transferred);
    }

    private static AddCharacterRequest NewCharacter(ProjectIdentification projectId, ProjectInfo projectInfo)
        => new(
            projectId,
            ParentCharacterGroupIds: [],
            new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
            FieldValues: FieldLayerContainer.Empty(projectInfo));

    private Task<ClaimIdentification> AddClaimAsync(
        UserIdentification playerId,
        ProjectIdentification projectId,
        CharacterIdentification characterId)
        => factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });
}
