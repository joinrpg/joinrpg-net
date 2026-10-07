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

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Игрок начинает онлайн-оплату взноса картой: <c>POST /payments/ClaimPayment</c>.
/// </summary>
/// <remarks>
/// Ручка мутирующая, поэтому смоук по GET-страницам (<see cref="AllGetPagesSmokeScenario"/>) её не
/// видит, а на проде на ней ловились ленивые загрузки (#5109). Банк здесь не нужен: при оплате
/// картой портал только подписывает сообщение и отдаёт страницу с формой, которая уводит браузер
/// на сайт банка, — сетевого вызова к эквайрингу на этом шаге нет.
///
/// Ленивые загрузки проверяет <c>LazyLoadAssertingHandler</c> на клиенте фабрики, сам тест
/// проверяет только, что оплата действительно началась.
/// </remarks>
public class OnlinePaymentInitiationScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const int Money = 500;

    [Fact]
    public async Task PlayerStartsCardPayment_GetsRedirectToBankAndProposedOperation()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        UserIdentification playerId;
        string playerEmail;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект с онлайн-оплатой");
            (playerId, playerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        }

        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            // Онлайн-оплату включает и мастер: админ нужен только чтобы включить её обратно после отключения.
            await sp.GetRequiredService<IProjectFinanceSettingsService>().CreatePaymentType(new CreatePaymentTypeRequest
            {
                ProjectId = projectId,
                TargetMasterId = null,
                TypeKind = PaymentTypeKind.Online,
                Name = null,
            });

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
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), playerEmail);

        var claimPage = $"{projectId.Value}/claim/{claimId.ClaimId}/edit";
        var token = await client.GetAntiforgeryTokenAsync(claimPage);

        var response = await client.PostFormAsync(
            "payments/ClaimPayment",
            token,
            ("ProjectId", projectId.Value.ToString()),
            ("ClaimId", claimId.ClaimId.ToString()),
            ("CommentDiscussionId", claimId.ClaimId.ToString()),
            ("Money", Money.ToString()),
            ("Method", nameof(PaymentMethod.BankCard)),
            ("OperationDate", DateTime.Today.ToString("yyyy-MM-dd")),
            ("CommentText", "Оплата взноса"),
            ("AcceptContract", "true"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        // Ошибку контроллер отдаёт тоже со статусом 200, поэтому успех — это форма ухода в банк.
        html.ShouldContain("id=\"pay\"");

        using var checkScope = factory.Services.CreateScope();
        var operation = checkScope.ServiceProvider.GetRequiredService<MyDbContext>().Set<FinanceOperation>()
            .Single(fo => fo.ProjectId == projectId.Value && fo.ClaimId == claimId.ClaimId);
        operation.OperationType.ShouldBe(FinanceOperationType.Online);
        operation.State.ShouldBe(FinanceOperationState.Proposed);
        operation.MoneyAmount.ShouldBe(Money);
    }
}
