using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Страница редактирования заявки не должна делать ленивых загрузок (#4960).
/// </summary>
/// <remarks>
/// На проде этот маршрут давал десятки догрузок на один запрос, причём их число росло
/// с числом комментариев и финансовых операций заявки — то есть это N+1 по строкам,
/// а не разовый добор. Поэтому сид намеренно создаёт несколько комментариев и несколько
/// платежей: на заявке с одним комментарием N+1 просто не виден.
///
/// Проверку делает не сам тест, а <c>LazyLoadAssertingHandler</c> на клиенте фабрики: замер
/// сверяется с <c>lazy-loads-baseline.json</c> (см. docs/lazy-loads-baseline.md). Здесь заявка без
/// поселения, и страница не делает ни одной догрузки; число в снапшоте пока держит смоук, где
/// проживание включено и карточка поселения ещё ходит по ленивым навигациям (#4964).
/// </remarks>
public class ClaimEditLazyLoadsScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    /// <summary>Комментариев в обсуждении заявки.</summary>
    private const int CommentCount = 5;

    /// <summary>Платежей по заявке.</summary>
    private const int PaymentCount = 3;

    [Fact]
    public async Task ClaimEditPage_WithCommentsAndPayments_DoesNotLazyLoad()
    {
        UserIdentification masterId;
        string masterEmail;
        ProjectIdentification projectId;
        UserIdentification playerId;
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, masterEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект со страницей заявки");
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }

        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: true);
        });

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            await sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято");
        });

        // Комментарии от обоих участников: у каждого автора свой User, и именно по авторам
        // на проде шла догрузка UserExtras.
        for (var i = 0; i < CommentCount; i++)
        {
            var authorId = i % 2 == 0 ? masterId : playerId;
            await factory.Services.RunAsAsync(authorId, async sp =>
            {
                await sp.GetRequiredService<IClaimService>().AddComment(
                    claimId,
                    parentCommentId: null,
                    isVisibleToPlayer: true,
                    commentText: $"Комментарий {i}");
            });
        }

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            await sp.GetRequiredService<IProjectFinanceSettingsService>().CreatePaymentType(
                new CreatePaymentTypeRequest
                {
                    ProjectId = projectId,
                    TargetMasterId = masterId,
                    TypeKind = PaymentTypeKind.Cash,
                    Name = null,
                });
        });

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            var paymentType = projectInfo.ProjectFinanceSettings.GetCashPaymentType(masterId)
                ?? throw new InvalidOperationException("В проекте нет наличного способа оплаты");
            var financeService = sp.GetRequiredService<IFinanceService>();
            for (var i = 0; i < PaymentCount; i++)
            {
                await financeService.FeeAcceptedOperation(new FeeAcceptedOperationRequest
                {
                    ClaimId = claimId.ClaimId,
                    Contents = $"Оплата {i}",
                    OperationDate = DateTime.UtcNow.AddDays(-i),
                    Money = 100,
                    PaymentTypeId = paymentType.PaymentTypeId,
                });
            }
        });

        var masterClient = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(), masterEmail);

        var response = await masterClient.GetAsync($"{projectId.Value}/claim/{claimId.ClaimId}/edit");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var document = await response.AsHtmlDocument();
        var text = WebUtility.HtmlDecode(
            document.DocumentNode.SelectSingleNode("//body")?.InnerText
            ?? throw new InvalidOperationException("Страница не содержит body"));

        // Без этого тест мог бы пройти на странице, где комментарии и платежи не отрисовались.
        text.ShouldContain($"Комментарий {CommentCount - 1}");
        text.ShouldContain("Оплата 0");
    }
}
