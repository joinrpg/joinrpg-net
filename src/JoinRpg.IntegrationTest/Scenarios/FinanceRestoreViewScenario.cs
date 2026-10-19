using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks.Fakes;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Блок «Ручные оплаты» страницы восстановления (DRP, #5631) через настоящий DI и SQL Server: план по
/// уведомлению, восстановление оплаты с прежним id на выбранный мастером тип, повтор и отказы. База
/// уведомлений в тестах — <see cref="FakeNotificationRepository"/>-синглтон, уведомление кладётся в него.
/// </summary>
public class FinanceRestoreViewScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    /// <summary>Между бэкапом и аварией из appsettings портала (раздел Drp).</summary>
    private static readonly DateTimeOffset PaidAt = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);
    private const int Money = 700;

    private sealed record Seed(
        UserIdentification MasterId,
        ProjectIdentification ProjectId,
        ClaimIdentification ClaimId,
        PaymentTypeIdentification CashId,
        int LostOperationId);

    [Fact]
    public async Task RestoresPaymentWithFormerId_RepeatIsAlreadyExisting()
    {
        var seed = await SeedAsync();
        await BlockProject(seed.ProjectId);
        var key = $"id:{seed.LostOperationId}";

        var plan = await RunAsMaster(seed.MasterId, client => client.GetPlan(seed.ProjectId));

        plan.UnavailableReason.ShouldBeNull();
        plan.HasOwnCash.ShouldBeTrue();
        plan.PaymentTypes.ShouldHaveSingleItem().PaymentTypeId.ShouldBe(seed.CashId.PaymentTypeId);
        var row = plan.Rows.ShouldHaveSingleItem();
        row.ClaimId.ShouldBe(seed.ClaimId);
        row.ClaimExists.ShouldBeTrue();
        row.CharacterName.ShouldNotBeNull();
        var operation = row.Operations.ShouldHaveSingleItem();
        operation.Key.ShouldBe(key);
        operation.Money.ShouldBe(Money);
        operation.CanApply.ShouldBeTrue();
        operation.SuggestedPaymentTypeId.ShouldBe(seed.CashId.PaymentTypeId);

        var request = new FinanceRestoreApplyRequest(seed.ClaimId, new Dictionary<string, int> { [key] = seed.CashId.PaymentTypeId });
        var result = await RunAsMaster(seed.MasterId, client => client.Apply(request));

        result.Inserted.ShouldBe(1);
        result.Conflicts.ShouldBe(0);
        result.Skipped.ShouldBeEmpty();
        using (var scope = factory.Services.CreateScope())
        {
            var restored = await scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<FinanceOperation>()
                .SingleAsync(f => f.CommentId == seed.LostOperationId);
            restored.ClaimId.ShouldBe(seed.ClaimId.ClaimId);
            restored.MoneyAmount.ShouldBe(Money);
            restored.PaymentTypeId.ShouldBe(seed.CashId.PaymentTypeId);
        }

        // Повтор: в плане — «уже есть», применение ничего не вставляет.
        var again = await RunAsMaster(seed.MasterId, client => client.GetPlan(seed.ProjectId));
        var againRow = again.Rows.ShouldHaveSingleItem();
        againRow.AlreadyExisting.ShouldBe(1);
        againRow.Operations.ShouldHaveSingleItem().CanApply.ShouldBeFalse();
        (await RunAsMaster(seed.MasterId, client => client.Apply(request))).Inserted.ShouldBe(0);
    }

    [Fact]
    public async Task MasterWithoutMoneyPermission_IsRefused()
    {
        var seed = await SeedAsync();
        UserIdentification claimsMasterId;
        using (var scope = factory.Services.CreateScope())
        {
            claimsMasterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }
        await factory.Services.RunAsAsync(seed.MasterId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = seed.ProjectId,
                UserId = claimsMasterId,
                Role = new("Мастер по заявкам"),
                Permissions = [Permission.CanManageClaims],
            }));
        await BlockProject(seed.ProjectId);

        var plan = await RunAsMaster(claimsMasterId, client => client.GetPlan(seed.ProjectId));
        plan.UnavailableReason.ShouldBe(ClaimRestoreUnavailableReason.NoAccess);
        plan.Rows.ShouldBeEmpty();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(() => RunAsMaster(claimsMasterId, client => client.Apply(
            new FinanceRestoreApplyRequest(seed.ClaimId, new Dictionary<string, int> { [$"id:{seed.LostOperationId}"] = seed.CashId.PaymentTypeId }))));
        await EnsureNotRestored(seed);
    }

    [Fact]
    public async Task NotBlockedProject_IsRefused()
    {
        var seed = await SeedAsync();

        var plan = await RunAsMaster(seed.MasterId, client => client.GetPlan(seed.ProjectId));
        plan.UnavailableReason.ShouldBe(ClaimRestoreUnavailableReason.ProjectNotBlocked);

        _ = await Should.ThrowAsync<JoinValidationException>(() => RunAsMaster(seed.MasterId, client => client.Apply(
            new FinanceRestoreApplyRequest(seed.ClaimId, new Dictionary<string, int> { [$"id:{seed.LostOperationId}"] = seed.CashId.PaymentTypeId }))));
        await EnsureNotRestored(seed);
    }

    private Task<T> RunAsMaster<T>(UserIdentification userId, Func<IFinanceRestoreClient, Task<T>> action)
        => factory.Services.RunAsAsync(userId, sp => action(sp.GetRequiredService<IFinanceRestoreClient>()));

    private async Task EnsureNotRestored(Seed seed)
    {
        using var scope = factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<FinanceOperation>()
            .AnyAsync(f => f.CommentId == seed.LostOperationId)).ShouldBeFalse();
    }

    /// <summary>
    /// Проект с наличными мастера, заявка игрока, «потерянный» id (ниже счётчика комментариев) и уведомление
    /// о приёме взноса мастером под этим id.
    /// </summary>
    private async Task<Seed> SeedAsync()
    {
        UserIdentification masterId;
        UserIdentification playerId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
        }

        var cashId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            await sp.GetRequiredService<IProjectFinanceSettingsService>().CreatePaymentType(new CreatePaymentTypeRequest
            {
                ProjectId = projectId,
                TargetMasterId = masterId,
                TypeKind = PaymentTypeKind.Cash,
                Name = null,
            });
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return projectInfo.ProjectFinanceSettings.GetCashPaymentType(masterId)!.PaymentTypeId;
        });

        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
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
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });
        var header = await factory.Services.RunAsAsync(masterId, async sp =>
            (await sp.GetRequiredService<IClaimsRepository>().GetClaimHeadersWithPlayer([claimId])).Single());

        int lostId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var discussionId = await db.Set<Claim>().Where(c => c.ClaimId == claimId.ClaimId).Select(c => c.CommentDiscussionId).SingleAsync();
            lostId = await BurnTwoCommentIdsAsync(db, projectId, discussionId, masterId);
        }

        // Как пишет ClaimNotificationTextBuilder: приветствие получателя, строка заявки, действие, сумма, текст.
        var playerName = header.Player.DisplayName.DisplayName;
        var body = $"""
            Добрый день, {playerName}
            Заявка {header.CharacterName} игрока {playerName} отмечено мастером Мастер

            **Оплачен взнос**

            Сумма операции: {Money}

            Принял наличными
            """;
        factory.Services.GetRequiredService<INotificationRepository>().ShouldBeOfType<FakeNotificationRepository>()
            .AddClaimNotification(new NotificationMessageForRecipient(
                new MarkdownString(body.Replace("\r\n", "\n")), masterId, $"Проект: {header.CharacterName}, игрок {playerName}", playerId,
                new ClaimCommentIdentification(claimId, lostId), PaidAt));

        return new Seed(masterId, projectId, claimId, cashId, lostId);
    }

    /// <summary>Сервиса блокировки пока нет — ставим флаг прямо в БД (как в <see cref="BlockedProjectScenario"/>).</summary>
    private async Task BlockProject(ProjectIdentification projectId)
    {
        using var scope = factory.Services.CreateScope();
        _ = await scope.ServiceProvider.GetRequiredService<MyDbContext>().Database.ExecuteSqlCommandAsync(
            "UPDATE dbo.Projects SET IsBlocked = 1 WHERE ProjectId = @p0",
            projectId.Value);
    }

    /// <summary>
    /// Расходует два значения IDENTITY таблицы комментариев вставками, которые тут же откатываются, и
    /// возвращает первое из них — оно строго ниже счётчика (как в <see cref="RestoredFinanceOperationsScenario"/>).
    /// </summary>
    private static Task<int> BurnTwoCommentIdsAsync(MyDbContext db, ProjectIdentification projectId, int discussionId, UserIdentification authorId)
    {
        const string sql = """
            SET NOCOUNT ON;
            DECLARE @first int;
            BEGIN TRANSACTION;
            INSERT INTO dbo.Comments (ProjectId, CommentDiscussionId, CreatedTime, LastEditTime, AuthorUserId, IsCommentByPlayer, IsVisibleToPlayer)
            VALUES (@p0, @p1, @p3, @p3, @p2, 0, 1);
            SET @first = CAST(SCOPE_IDENTITY() AS int);
            INSERT INTO dbo.Comments (ProjectId, CommentDiscussionId, CreatedTime, LastEditTime, AuthorUserId, IsCommentByPlayer, IsVisibleToPlayer)
            VALUES (@p0, @p1, @p3, @p3, @p2, 0, 1);
            ROLLBACK TRANSACTION;
            SELECT @first;
            """;

        return db.Database.SqlQuery<int>(sql, projectId.Value, discussionId, authorId.Value, DateTime.UtcNow).SingleAsync();
    }
}
