using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Claims.Comments;
using JoinRpg.Web.Claims.Finance;
using CommentExtraAction = JoinRpg.DomainTypes.Characters.Claims.CommentExtraAction;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Ручки диалога модерации финансовой операции (<c>/webapi/{projectId}/finance-moderation/</c>, #3366):
/// комментарий идёт путём обычного комментария, подтверждение и отклонение — модерацией.
/// </summary>
/// <remarks>
/// Само поведение модерации покрыто юнит-тестами <c>ClaimServiceImplTest</c>. Здесь проверяется, что
/// ручки доходят до нужной операции сервиса с нужными аргументами и что отказы приходят текстом,
/// который диалог покажет мастеру.
/// </remarks>
public class FinanceModerationApiScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    /// <summary>Любая страница с формой: antiforgery-токен привязан к cookie клиента, а не к странице.</summary>
    private const string AntiforgeryTokenPage = "game/create";

    [Fact]
    public async Task Approve_WithoutText_ApprovesOperation()
    {
        var seed = await SeedAsync();
        var master = await seed.MasterClientAsync(factory);

        var response = await PostAsync(master, seed, "approve", new FinanceModerationDecisionRequest(CommentText: null));

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var (operation, reply) = LoadOperationAndLastReply(seed);
        operation.State.ShouldBe(FinanceOperationState.Approved);
        reply.ShouldNotBeNull().ExtraAction.ShouldBe(CommentExtraAction.ApproveFinance);
        reply.IsVisibleToPlayer.ShouldBeTrue();
    }

    [Fact]
    public async Task Approve_AlreadyModerated_IsRejectedWithReason()
    {
        var seed = await SeedAsync();
        var master = await seed.MasterClientAsync(factory);
        (await PostAsync(master, seed, "approve", new FinanceModerationDecisionRequest("ок"))).EnsureSuccessStatusCode();

        var response = await PostAsync(master, seed, "approve", new FinanceModerationDecisionRequest("ещё раз"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("уже подтверждена или отклонена");
    }

    [Fact]
    public async Task Decline_WithoutText_IsRejected_AndOperationStaysProposed()
    {
        var seed = await SeedAsync();
        var master = await seed.MasterClientAsync(factory);

        var response = await PostAsync(master, seed, "decline", new FinanceModerationDecisionRequest("  "));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        LoadOperationAndLastReply(seed).Operation.State.ShouldBe(FinanceOperationState.Proposed);
    }

    [Fact]
    public async Task Decline_WithText_DeclinesOperation()
    {
        var seed = await SeedAsync();
        var master = await seed.MasterClientAsync(factory);

        var response = await PostAsync(master, seed, "decline", new FinanceModerationDecisionRequest("Денег не пришло"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var (operation, reply) = LoadOperationAndLastReply(seed);
        operation.State.ShouldBe(FinanceOperationState.Declined);
        reply.ShouldNotBeNull().ExtraAction.ShouldBe(CommentExtraAction.RejectFinance);
    }

    /// <summary>
    /// Просто комментарий — общей ручкой комментариев к заявке: ответ на комментарий операции без решения по ней,
    /// видимость выбирает мастер.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Comment_RepliesWithoutModeration(bool hideFromPlayer)
    {
        var seed = await SeedAsync();
        var master = await seed.MasterClientAsync(factory);

        var response = await PostCommentAsync(master, seed.ReplyToOperation("Проверю выписку", isVisibleToPlayer: !hideFromPlayer));

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var (operation, reply) = LoadOperationAndLastReply(seed);
        operation.State.ShouldBe(FinanceOperationState.Proposed);
        reply.ShouldNotBeNull().ExtraAction.ShouldBeNull();
        reply.IsVisibleToPlayer.ShouldBe(!hideFromPlayer);
    }

    [Fact]
    public async Task Comment_WithoutText_IsRejected_AndNothingIsAdded()
    {
        var seed = await SeedAsync();
        var master = await seed.MasterClientAsync(factory);

        var response = await PostCommentAsync(master, seed.ReplyToOperation("", isVisibleToPlayer: true));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        LoadOperationAndLastReply(seed).Reply.ShouldBeNull();
    }

    /// <summary>
    /// Ручка комментариев открыта и игроку его заявки, но скрыть комментарий от игрока может только мастер.
    /// </summary>
    [Fact]
    public async Task Comment_ByPlayer_VisibleIsAdded_HiddenIsForbidden()
    {
        var seed = await SeedAsync();
        var player = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), seed.PlayerEmail);

        var hidden = await PostCommentAsync(player, seed.ReplyToOperation("секрет", isVisibleToPlayer: false));
        hidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        LoadOperationAndLastReply(seed).Reply.ShouldBeNull();

        var visible = await PostCommentAsync(player, seed.ReplyToOperation("Отдал Пете", isVisibleToPlayer: true));
        visible.StatusCode.ShouldBe(HttpStatusCode.OK, await visible.Content.ReadAsStringAsync());
        LoadOperationAndLastReply(seed).Reply.ShouldNotBeNull().IsVisibleToPlayer.ShouldBeTrue();
    }

    [Fact]
    public async Task Comment_ParentFromOtherClaim_IsRejected()
    {
        var seed = await SeedAsync();
        var master = await seed.MasterClientAsync(factory);
        var otherClaim = new ClaimIdentification(seed.OperationId.ProjectId, seed.OperationId.ClaimId.ClaimId + 100_000);

        var response = await PostCommentAsync(master, seed.ReplyToOperation("ок", isVisibleToPlayer: true) with { ClaimId = otherClaim });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Мастер без права на деньги и не владелец кассы — штатный отказ, который диалог покажет текстом.
    /// </summary>
    [Fact]
    public async Task Approve_ByMasterWithoutMoneyPermission_IsForbiddenWithReason()
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
                ProjectId = seed.OperationId.ProjectId,
                UserId = claimsMasterId,
                Role = new("Мастер по заявкам"),
                Permissions = [Permission.CanManageClaims],
            }));
        var claimsMaster = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), claimsMasterEmail);

        var response = await PostAsync(claimsMaster, seed, "approve", new FinanceModerationDecisionRequest("ок"));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Недостаточно прав");
        LoadOperationAndLastReply(seed).Operation.State.ShouldBe(FinanceOperationState.Proposed);
    }

    [Fact]
    public async Task Approve_ByPlayer_IsRejected_AndOperationStaysProposed()
    {
        var seed = await SeedAsync();
        var player = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), seed.PlayerEmail);

        var response = await PostAsync(player, seed, "approve", new FinanceModerationDecisionRequest("сам себе"));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        LoadOperationAndLastReply(seed).Operation.State.ShouldBe(FinanceOperationState.Proposed);
    }

    private static async Task<HttpResponseMessage> PostCommentAsync(HttpClient client, AddClaimCommentRequest request)
    {
        var token = await client.GetAntiforgeryTokenAsync(AntiforgeryTokenPage);
        return await client.PostJsonWithTokenHeaderAsync("webapi/claim-comment/AddComment", request, token);
    }

    /// <summary>
    /// Кнопка диалога на странице заявки — в комментарии с операцией. Остров пререндерится на сервере, так что
    /// заодно видно, что его параметр собирается без ошибок и лишних загрузок.
    /// </summary>
    [Fact]
    public async Task ClaimPage_ShowsModerationButton_OnlyToModerator()
    {
        var seed = await SeedAsync();
        var master = await seed.MasterClientAsync(factory);
        var player = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), seed.PlayerEmail);
        var claimPage = $"{seed.OperationId.ProjectId.Value}/claim/{seed.OperationId.ClaimId.ClaimId}/edit";

        var masterPage = await master.GetAsync(claimPage);
        var playerPage = await player.GetAsync(claimPage);

        masterPage.StatusCode.ShouldBe(HttpStatusCode.OK);
        // Текст из окна острова, а не только с кнопки: у прежней ссылки-раскрывашки подпись была та же
        (await PageTextAsync(masterPage)).ShouldContain("Оставить комментарий");
        playerPage.StatusCode.ShouldBe(HttpStatusCode.OK);
        var playerText = await PageTextAsync(playerPage);
        playerText.ShouldNotContain("Оставить комментарий");
        playerText.ShouldContain("Ждет одобрения мастером");
    }

    /// <summary>Текст страницы: пререндер островов кодирует кириллицу HTML-сущностями.</summary>
    private static async Task<string> PageTextAsync(HttpResponseMessage response)
        => HtmlAgilityPack.HtmlEntity.DeEntitize((await response.AsHtmlDocument()).DocumentNode.InnerText);

    private static async Task<HttpResponseMessage> PostAsync<TBody>(HttpClient client, Seed seed, string action, TBody body)
    {
        var token = await client.GetAntiforgeryTokenAsync(AntiforgeryTokenPage);
        return await client.PostJsonWithTokenHeaderAsync(
            $"webapi/{seed.OperationId.ProjectId.Value}/finance-moderation/{action}"
                + $"?claimId={seed.OperationId.ClaimId.ClaimId}&operationId={seed.OperationId.FinanceOperationId}",
            body,
            token);
    }

    /// <summary>
    /// Операция и последний ответ на её комментарий — новой базой, а не контекстом запроса.
    /// </summary>
    private (FinanceOperation Operation, Comment? Reply) LoadOperationAndLastReply(Seed seed)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var operationId = seed.OperationId.FinanceOperationId;
        var operation = db.Set<FinanceOperation>().Single(fo => fo.CommentId == operationId);
        var reply = db.Set<Comment>()
            .Where(c => c.ParentCommentId == operationId)
            .OrderByDescending(c => c.CommentId)
            .FirstOrDefault();
        return (operation, reply);
    }

    private sealed record Seed(FinanceOperationIdentification OperationId, UserIdentification MasterId, string MasterEmail, string PlayerEmail)
    {
        /// <summary>Ответ на комментарий, в котором заведена операция.</summary>
        public AddClaimCommentRequest ReplyToOperation(string text, bool isVisibleToPlayer)
            => new(OperationId.ClaimId, new ClaimCommentIdentification(OperationId.ClaimId, OperationId.FinanceOperationId), text, isVisibleToPlayer);

        public Task<HttpClient> MasterClientAsync(JoinApplicationFactory factory)
            => TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), MasterEmail);
    }

    /// <summary>
    /// Проект с кассой мастера и заявкой, по которой игрок заявил платёж — операция ждёт модерации.
    /// </summary>
    private async Task<Seed> SeedAsync()
    {
        UserIdentification masterId;
        string masterEmail;
        UserIdentification playerId;
        string playerEmail;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, masterEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            (playerId, playerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для модерации взносов");
        }

        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
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

        // Платит сам игрок на кассу мастера — такая операция создаётся неподтверждённой.
        await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IFinanceService>().FeeAcceptedOperation(new FeeAcceptedOperationRequest
            {
                ClaimId = claimId.ClaimId,
                Contents = "Отдал наличными",
                OperationDate = DateTime.UtcNow.Date,
                Money = 500,
                PaymentTypeId = projectInfo.ProjectFinanceSettings.GetCashPaymentType(masterId)!.PaymentTypeId,
            });
        });

        using var checkScope = factory.Services.CreateScope();
        var operation = checkScope.ServiceProvider.GetRequiredService<MyDbContext>().Set<FinanceOperation>()
            .Single(fo => fo.ProjectId == projectId.Value && fo.ClaimId == claimId.ClaimId);
        operation.State.ShouldBe(FinanceOperationState.Proposed, "Платёж игрока должен ждать модерации");

        return new Seed(new FinanceOperationIdentification(claimId, operation.CommentId), masterId, masterEmail, playerEmail);
    }
}
