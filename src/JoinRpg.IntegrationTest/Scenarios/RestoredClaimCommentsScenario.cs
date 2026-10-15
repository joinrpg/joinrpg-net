using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Drp;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Восстановление комментариев к заявке по уведомлениям (DRP, #5492) на настоящем SQL Server:
/// <c>SET IDENTITY_INSERT</c> для прежних id, счётчик IDENTITY, который вставка ниже текущего значения
/// двигать не должна, паритет строк с доменным добавлением комментария и дата последнего обновления
/// проекта, которую восстановление обязано поднять (ADR023).
/// </summary>
public class RestoredClaimCommentsScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    private static readonly DateTime BackupAt = new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset MasterAnsweredAt = new(2026, 7, 2, 10, 0, 0, TimeSpan.FromHours(3));
    // Доли секунды мельче шага колонки datetime: повтор всё равно должен узнаваться.
    private static readonly DateTimeOffset PlayerWroteAt = new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.FromHours(3)).AddTicks(1_234_567);
    private static readonly DateTimeOffset MasterNotedAt = new(2026, 7, 3, 10, 0, 0, TimeSpan.FromHours(3));
    private const string ReferenceText = "Ответ {мастера}";

    [Fact]
    public async Task RestoreClaimComments_InsertsWithGivenIds_LikeDomainComment_AndBumpsProjectUpdate()
    {
        // 1. Мастер, проект, два игрока с заявками
        UserIdentification masterId;
        ProjectIdentification projectId;
        UserIdentification playerId;
        UserIdentification otherPlayerId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            otherPlayerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }

        var (characterId, otherCharacterId) = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            var characterService = sp.GetRequiredService<ICharacterService>();
            AddCharacterRequest Request() => new(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo));
            return (await characterService.AddCharacter(Request()), await characterService.AddCharacter(Request()));
        });

        var claimId = await AddClaim(playerId, characterId);
        var otherClaimId = await AddClaim(otherPlayerId, otherCharacterId);

        // 2. Эталон — доменное добавление видимого комментария мастером; и комментарий в чужой заявке
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            await claimService.AddComment(claimId, parentCommentId: null, isVisibleToPlayer: true, ReferenceText);
            await claimService.AddComment(otherClaimId, parentCommentId: null, isVisibleToPlayer: true, "Чужая заявка");
        });

        // 3. Дырка в счётчике комментариев (как после сдвига счётчиков, #5460); проект «с майскими датами»
        int lostCommentId;
        int identityBefore;
        CommentRow reference;
        int foreignCommentId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var discussionId = await GetDiscussionIdAsync(db, claimId);
            // Последний в обсуждении: первым идёт комментарий подачи заявки. Читается до состаривания,
            // чтобы сравнивать с тем, как время правки пишет домен.
            var referenceCommentId = await db.Set<Comment>().Where(c => c.CommentDiscussionId == discussionId).MaxAsync(c => c.CommentId);
            reference = await GetCommentAsync(db, referenceCommentId);
            var otherDiscussionId = await GetDiscussionIdAsync(db, otherClaimId);
            foreignCommentId = await db.Set<Comment>().Where(c => c.CommentDiscussionId == otherDiscussionId).MaxAsync(c => c.CommentId);

            lostCommentId = await BurnTwoCommentIdsAsync(db, projectId, discussionId, masterId);
            identityBefore = await GetCommentsIdentityAsync(db);
            identityBefore.ShouldBeGreaterThan(lostCommentId);

            _ = await BackdateProjectAsync(db, projectId);
        }
        await BlockProject(projectId);
        (await GetProjectLastUpdatedAsync(masterId, projectId)).ShouldBe(DateOnly.FromDateTime(BackupAt));

        RestoredClaimComment[] comments =
        [
            new(lostCommentId, masterId, IsCommentByPlayer: false, IsVisibleToPlayer: true, ExtraAction: null, MasterAnsweredAt, ReferenceText),
            // До 2026-06-06 id в уведомлениях не было — получит новый.
            new(null, playerId, IsCommentByPlayer: true, IsVisibleToPlayer: true, ExtraAction: null, PlayerWroteAt, "Вопрос игрока"),
            new(null, masterId, IsCommentByPlayer: false, IsVisibleToPlayer: false, CommentExtraAction.ChangeResponsible, MasterNotedAt, "Ответственный сменился"),
        ];

        // 4. Восстановление
        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IClaimRestoreService>().RestoreClaimComments(claimId, comments));

        result.Inserted.ShouldBe(3);
        result.AlreadyExisted.ShouldBe(0);
        result.Conflicts.ShouldBeEmpty();

        // 5. Строки как у доменного комментария, id сохранён; счётчик сдвинули только комментарии без id
        // и запись в истории — вставка с прежним id его не двигает
        DateTime claimUpdatedAt;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            var restored = await GetCommentAsync(db, lostCommentId);
            restored.CreatedTime.ShouldBe(MasterAnsweredAt.UtcDateTime);
            restored.AuthorUserId.ShouldBe(masterId.Value);
            Comparable(restored).ShouldBe(Comparable(reference));

            var withoutId = await GetCommentAsync(db, identityBefore + 1);
            withoutId.Text.ShouldBe("Вопрос игрока");
            withoutId.IsCommentByPlayer.ShouldBeTrue();
            withoutId.AuthorUserId.ShouldBe(playerId.Value);
            withoutId.ExtraAction.ShouldBeNull();

            var withAction = await GetCommentAsync(db, identityBefore + 2);
            withAction.ExtraAction.ShouldBe((int)CommentExtraAction.ChangeResponsible);
            withAction.IsVisibleToPlayer.ShouldBeFalse();
            (await GetCommentsIdentityAsync(db)).ShouldBe(identityBefore + 3);

            var claim = await db.Set<Claim>().AsNoTracking().SingleAsync(c => c.ClaimId == claimId.ClaimId);
            claim.LastPlayerCommentAt.ShouldBe(PlayerWroteAt);
            // Не «восстановитель, сейчас» из записи в истории — иначе сломается фильтр «без ответа».
            claim.LastMasterCommentAt.ShouldBe(MasterNotedAt);
            claim.LastMasterCommentBy_Id.ShouldBe(masterId.Value);
            claim.LastVisibleMasterCommentAt.ShouldBe(MasterAnsweredAt);
            claim.LastVisibleMasterCommentBy_Id.ShouldBe(masterId.Value);
            claim.LastUpdateDateTime.ShouldBeGreaterThanOrEqualTo(before);
            claimUpdatedAt = claim.LastUpdateDateTime;

            var history = await db.Set<Comment>().AsNoTracking()
                .Where(c => c.CommentDiscussionId == claim.CommentDiscussionId && c.ExtraAction == CommentExtraAction.RestoredFromNotifications)
                .ToListAsync();
            history.ShouldHaveSingleItem().IsVisibleToPlayer.ShouldBeFalse();
        }

        // Дата последнего обновления проекта — сейчас, хотя комментарии исторические (ADR023).
        (await GetProjectLastUpdatedAsync(masterId, projectId)).ShouldBeGreaterThanOrEqualTo(DateOnly.FromDateTime(before));

        // 6. Повтор — ничего не вставляет, в историю не пишет, заявку не трогает
        var repeat = await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IClaimRestoreService>().RestoreClaimComments(claimId, comments));
        repeat.Inserted.ShouldBe(0);
        // В том числе комментарий без id со временем мельче шага datetime.
        repeat.AlreadyExisted.ShouldBe(3);
        repeat.Conflicts.ShouldBeEmpty();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var discussionId = await GetDiscussionIdAsync(db, claimId);
            // Подача заявки, эталон, три восстановленных и запись в истории.
            (await db.Set<Comment>().CountAsync(c => c.CommentDiscussionId == discussionId)).ShouldBe(6);
            (await db.Set<Claim>().SingleAsync(c => c.ClaimId == claimId.ClaimId)).LastUpdateDateTime.ShouldBe(claimUpdatedAt);
            (await GetCommentsIdentityAsync(db)).ShouldBe(identityBefore + 3);
        }

        // 7. Конфликты: id чужой заявки, id не ниже счётчика (и ровно на нём) — пропущены, счётчик не сдвинут
        int identity;
        using (var scope = factory.Services.CreateScope())
        {
            // Граница: значение счётчика выдано, но откачено — строки нет, и всё же это не потерянный id.
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            _ = await BurnTwoCommentIdsAsync(db, projectId, await GetDiscussionIdAsync(db, claimId), masterId);
            identity = await GetCommentsIdentityAsync(db);
            (await db.Set<Comment>().AnyAsync(c => c.CommentId == identity)).ShouldBeFalse();
        }
        var conflicts = await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IClaimRestoreService>().RestoreClaimComments(claimId,
            [
                new(foreignCommentId, playerId, true, true, null, PlayerWroteAt, "Занятый id"),
                new(identity, playerId, true, true, null, PlayerWroteAt, "Id счётчика"),
                new(identity + 1000, playerId, true, true, null, PlayerWroteAt, "Id выше счётчика"),
            ]));
        conflicts.Inserted.ShouldBe(0);
        conflicts.AlreadyExisted.ShouldBe(0);
        conflicts.Conflicts.ShouldBe([foreignCommentId, identity, identity + 1000]);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            (await db.Set<Comment>().AnyAsync(c => c.CommentId == identity || c.CommentId == identity + 1000)).ShouldBeFalse();
            (await GetCommentAsync(db, foreignCommentId)).Text.ShouldBe("Чужая заявка");
            (await GetCommentsIdentityAsync(db)).ShouldBe(identity);
        }

        // 8. На уровне репозитория — те же исходы
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var repository = sp.GetRequiredService<IUnitOfWork>().GetCharacterAggregateWriteRepository();
            Task<RestoredCommentInsertResult> Insert(int? id) => repository.InsertRestoredClaimComment(new Data.Interfaces.Characters.RestoredClaimCommentRow(
                claimId, id, masterId, false, true, null, MasterAnsweredAt.UtcDateTime, ReferenceText));

            (await Insert(lostCommentId)).ShouldBe(RestoredCommentInsertResult.AlreadyExists);
            (await Insert(foreignCommentId)).ShouldBe(RestoredCommentInsertResult.TakenElsewhere);
            (await Insert(identity)).ShouldBe(RestoredCommentInsertResult.NotLostId);
            // Без id повтор узнаётся по автору, времени и тексту.
            (await Insert(null)).ShouldBe(RestoredCommentInsertResult.AlreadyExists);
        });

        // 9. Обрыв: прошлый вызов вставил комментарии и упал до операции над заявкой. Повтор ничего не
        // вставляет и в историю не пишет, но дату обновления проекта поднимает — сами комментарии
        // несут исторические даты, и без LastUpdateDateTime = сейчас проект остался бы «майским».
        using (var scope = factory.Services.CreateScope())
        {
            _ = await BackdateProjectAsync(scope.ServiceProvider.GetRequiredService<MyDbContext>(), projectId);
        }
        (await GetProjectLastUpdatedAsync(masterId, projectId)).ShouldBe(DateOnly.FromDateTime(BackupAt));

        RestoredClaimComment[] interrupted =
        [
            new(null, otherPlayerId, IsCommentByPlayer: true, IsVisibleToPlayer: true, ExtraAction: null, PlayerWroteAt, "Вопрос до обрыва"),
            new(null, masterId, IsCommentByPlayer: false, IsVisibleToPlayer: true, ExtraAction: null, MasterAnsweredAt, "Ответ до обрыва"),
        ];
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var repository = sp.GetRequiredService<IUnitOfWork>().GetCharacterAggregateWriteRepository();
            foreach (var comment in interrupted)
            {
                (await repository.InsertRestoredClaimComment(new Data.Interfaces.Characters.RestoredClaimCommentRow(
                    otherClaimId, comment.CommentId, comment.Author, comment.IsCommentByPlayer, comment.IsVisibleToPlayer,
                    comment.ExtraAction, comment.CreatedAt.UtcDateTime, comment.Text)))
                    .ShouldBe(RestoredCommentInsertResult.Inserted);
            }
        });
        // Сами вставленные комментарии поднимают дату только до своей исторической.
        (await GetProjectLastUpdatedAsync(masterId, projectId)).ShouldBe(DateOnly.FromDateTime(MasterAnsweredAt.UtcDateTime));

        var resumeBefore = DateTime.UtcNow.AddSeconds(-1);
        var resumed = await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IClaimRestoreService>().RestoreClaimComments(otherClaimId, interrupted));
        resumed.Inserted.ShouldBe(0);
        resumed.AlreadyExisted.ShouldBe(2);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var otherClaim = await db.Set<Claim>().AsNoTracking().SingleAsync(c => c.ClaimId == otherClaimId.ClaimId);
            (await db.Set<Comment>().AnyAsync(c => c.CommentDiscussionId == otherClaim.CommentDiscussionId
                && c.ExtraAction == CommentExtraAction.RestoredFromNotifications)).ShouldBeFalse();
            otherClaim.LastPlayerCommentAt.ShouldBe(PlayerWroteAt);
            otherClaim.LastVisibleMasterCommentAt.ShouldBe(MasterAnsweredAt);
            otherClaim.LastUpdateDateTime.ShouldBeGreaterThanOrEqualTo(resumeBefore);
        }
        (await GetProjectLastUpdatedAsync(masterId, projectId)).ShouldBeGreaterThanOrEqualTo(DateOnly.FromDateTime(resumeBefore));
    }

    private Task<ClaimIdentification> AddClaim(UserIdentification playerId, CharacterIdentification characterId)
        => factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(characterId.ProjectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });

    /// <summary>
    /// Сервиса блокировки пока нет — ставим флаг прямо в БД (как в <see cref="BlockedProjectScenario"/>).
    /// </summary>
    private async Task BlockProject(ProjectIdentification projectId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        _ = await db.Database.ExecuteSqlCommandAsync(
            "UPDATE dbo.Projects SET IsBlocked = 1 WHERE ProjectId = @p0",
            projectId.Value);
    }

    /// <summary>
    /// Дата последнего обновления проекта — та, что считает <c>GetProjectWithLastUpdateQuery</c>.
    /// </summary>
    private Task<DateOnly> GetProjectLastUpdatedAsync(UserIdentification masterId, ProjectIdentification projectId)
        => factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projects = await sp.GetRequiredService<IUnitOfWork>().GetProjectRepository()
                .GetProjectsBySpecification(ProjectListSpecification.AllProjectsWithMasterAccess(masterId));
            return projects.Single(p => p.ProjectId == projectId).LastUpdatedAt;
        });

    /// <summary>
    /// Все метки, из которых складывается дата последнего обновления проекта, — на дату бэкапа: так
    /// выглядит проект, восстановленный из майского бэкапа.
    /// </summary>
    private static Task<int> BackdateProjectAsync(MyDbContext db, ProjectIdentification projectId)
        => db.Database.ExecuteSqlCommandAsync(
            """
            UPDATE dbo.Comments SET CreatedTime = @p1, LastEditTime = @p1 WHERE ProjectId = @p0;
            UPDATE dbo.Characters SET UpdatedAt = @p1 WHERE ProjectId = @p0;
            UPDATE dbo.CharacterGroups SET UpdatedAt = @p1 WHERE ProjectId = @p0;
            UPDATE dbo.PlotFolders SET ModifiedDateTime = @p1 WHERE ProjectId = @p0;
            UPDATE dbo.PlotElements SET ModifiedDateTime = @p1 WHERE ProjectId = @p0;
            UPDATE dbo.Claims SET LastUpdateDateTime = @p1, LastPlayerCommentAt = @p1, LastMasterCommentAt = @p1,
                LastVisibleMasterCommentAt = @p1
            WHERE ProjectId = @p0;
            """,
            projectId.Value,
            BackupAt);

    private static Task<int> GetDiscussionIdAsync(MyDbContext db, ClaimIdentification claimId)
        => db.Set<Claim>().Where(c => c.ClaimId == claimId.ClaimId).Select(c => c.CommentDiscussionId).SingleAsync();

    /// <summary>
    /// Расходует два значения IDENTITY таблицы комментариев вставками, которые тут же откатываются, и
    /// возвращает первое из них — оно строго ниже счётчика. Переменная откат транзакции переживает.
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

    private static Task<int> GetCommentsIdentityAsync(MyDbContext db)
        => db.Database.SqlQuery<int>("SELECT CAST(IDENT_CURRENT('dbo.Comments') AS int)").SingleAsync();

    private static Task<CommentRow> GetCommentAsync(MyDbContext db, int commentId)
        => db.Database.SqlQuery<CommentRow>(
                """
                SELECT c.ProjectId, c.CommentDiscussionId, c.ParentCommentId, c.AuthorUserId, c.IsCommentByPlayer,
                    c.IsVisibleToPlayer, c.ExtraAction, c.CreatedTime, c.LastEditTime, t.Text_Contents AS Text
                FROM dbo.Comments c
                JOIN dbo.CommentTexts t ON t.CommentId = c.CommentId
                WHERE c.CommentId = @p0
                """,
                commentId)
            .SingleAsync();

    /// <summary>Значимые колонки комментария — без id и дат (у эталона «сейчас», у восстановленного — из уведомления).</summary>
    private static object Comparable(CommentRow row) => new
    {
        row.ProjectId,
        row.CommentDiscussionId,
        row.ParentCommentId,
        row.AuthorUserId,
        row.IsCommentByPlayer,
        row.IsVisibleToPlayer,
        row.ExtraAction,
        row.Text,
        EditedAtCreation = row.CreatedTime == row.LastEditTime,
    };

    private sealed class CommentRow
    {
        public int ProjectId { get; set; }
        public int CommentDiscussionId { get; set; }
        public int? ParentCommentId { get; set; }
        public int AuthorUserId { get; set; }
        public bool IsCommentByPlayer { get; set; }
        public bool IsVisibleToPlayer { get; set; }
        public int? ExtraAction { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastEditTime { get; set; }
        public string? Text { get; set; }
    }
}
