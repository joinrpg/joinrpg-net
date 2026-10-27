using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Списки финансовых операций проекта: «Взносы — на модерации» (<c>money/Moderation</c>) и все операции
/// (<c>money/Operations</c>), #5852.
/// </summary>
/// <remarks>
/// Строки строятся из снимков заявок и метаданных проекта, без навигаций операции. Ленивые загрузки здесь
/// отдельно не проверяются: страж на клиенте фабрики требует ноль для маршрута, которого нет в снапшоте.
/// Поэтому в сиде две заявки разных игроков — N+1 по строкам не спрятался бы за одной строкой списка.
/// </remarks>
public class FinanceOperationListScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const string ModerationButton = "Подтвердить / отклонить";

    [Fact]
    public async Task Moderation_ShowsPendingPayments_WithModerationButton()
    {
        var seed = await SeedAsync();
        var master = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), seed.MasterEmail);

        var response = await master.GetAsync($"{seed.ProjectId.Value}/money/Moderation");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var text = await PageTextAsync(response);
        text.ShouldContain(ModerationButton);
        foreach (var player in seed.PlayerNames)
        {
            text.ShouldContain(player);
        }
    }

    /// <summary>Мастер без права на деньги и не владелец кассы список видит, но кнопки модерации у него нет.</summary>
    [Fact]
    public async Task Moderation_ByMasterWithoutMoneyPermission_HasNoModerationButton()
    {
        var seed = await SeedAsync();
        UserIdentification claimsMasterId;
        string claimsMasterEmail;
        using (var scope = factory.Services.CreateScope())
        {
            (claimsMasterId, claimsMasterEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        }
        await factory.Services.RunAsAsync(seed.MasterId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = seed.ProjectId,
                UserId = claimsMasterId,
                Role = new("Мастер по заявкам"),
                Permissions = [Permission.CanManageClaims],
            }));
        var claimsMaster = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), claimsMasterEmail);

        var response = await claimsMaster.GetAsync($"{seed.ProjectId.Value}/money/Moderation");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var text = await PageTextAsync(response);
        text.ShouldNotContain(ModerationButton);
        foreach (var player in seed.PlayerNames)
        {
            text.ShouldContain(player);
        }
    }

    [Fact]
    public async Task Operations_ShowsApprovedPayments()
    {
        var seed = await SeedAsync();
        var master = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), seed.MasterEmail);

        var response = await master.GetAsync($"{seed.ProjectId.Value}/money/Operations");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var text = await PageTextAsync(response);
        text.ShouldNotContain(ModerationButton);
        foreach (var player in seed.PlayerNames)
        {
            text.ShouldContain(player);
        }
    }

    /// <summary>Текст страницы: пререндер островов кодирует кириллицу HTML-сущностями.</summary>
    private static async Task<string> PageTextAsync(HttpResponseMessage response)
        => HtmlAgilityPack.HtmlEntity.DeEntitize((await response.AsHtmlDocument()).DocumentNode.InnerText);

    /// <param name="PlayerNames">Отображаемые имена игроков: без ФИО в профиле это часть email до «@».</param>
    private sealed record Seed(ProjectIdentification ProjectId, UserIdentification MasterId, string MasterEmail, IReadOnlyCollection<string> PlayerNames);

    /// <summary>
    /// Проект с кассой мастера и двумя заявками разных игроков. По каждой заявке игрок заявил платёж
    /// (ждёт модерации), а мастер отметил свой (подтверждён сразу).
    /// </summary>
    private async Task<Seed> SeedAsync()
    {
        UserIdentification masterId;
        string masterEmail;
        ProjectIdentification projectId;
        var players = new List<(UserIdentification Id, string Email)>();
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, masterEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            for (var i = 0; i < 2; i++)
            {
                players.Add(await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider));
            }
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для списков взносов");
        }

        await factory.Services.RunAsAsync(masterId, async sp =>
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
        });

        foreach (var (playerId, _) in players)
        {
            var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
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

            await factory.Services.RunAsAsync(masterId, sp => sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято"));

            // Игрок платит на кассу мастера — операция ждёт модерации; мастер отмечает сам — подтверждена сразу.
            await AddPaymentAsync(playerId, claimId, masterId, "Отдал наличными");
            await AddPaymentAsync(masterId, claimId, masterId, "Принял наличными");
        }

        return new Seed(projectId, masterId, masterEmail, [.. players.Select(p => p.Email.Split('@')[0])]);

        Task AddPaymentAsync(UserIdentification author, ClaimIdentification claimId, UserIdentification cashOwner, string contents)
            => factory.Services.RunAsAsync(author, async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
                await sp.GetRequiredService<IFinanceService>().FeeAcceptedOperation(new FeeAcceptedOperationRequest
                {
                    ClaimId = claimId.ClaimId,
                    Contents = contents,
                    OperationDate = DateTime.UtcNow.Date,
                    Money = 500,
                    PaymentTypeId = projectInfo.ProjectFinanceSettings.GetCashPaymentType(cashOwner)!.PaymentTypeId,
                });
            });
    }
}
