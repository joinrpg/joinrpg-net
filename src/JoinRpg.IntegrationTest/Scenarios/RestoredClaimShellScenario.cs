using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Вставка «оболочки» потерянной заявки с прежним идентификатором (DRP, #5481) на настоящем
/// SQL Server: <c>SET IDENTITY_INSERT</c>, обсуждение со своим обычным id и счётчик IDENTITY,
/// который явная вставка ниже текущего значения двигать не должна.
/// </summary>
public class RestoredClaimShellScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    private static readonly DateTime CreatedAt = new(2026, 6, 1, 7, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task InsertRestoredClaimShell_InsertsClaimWithGivenId_AndKeepsIdentity()
    {
        // 1. Мастер, проект, игрок, персонаж
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

        // 2. Дырка в счётчике: так в бою выглядит id заявки, поданной после бэкапа, — счётчик
        // сдвинут вперёд (#5460), а самой строки нет. Две вставки в откатываемой транзакции
        // расходуют два значения IDENTITY; первое и будет «потерянным» id — строго ниже счётчика.
        int lostClaimId;
        int identityBefore;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            lostClaimId = await BurnTwoClaimIdsAsync(db, projectId, characterId, playerId, masterId);
            identityBefore = await GetClaimsIdentityAsync(db);

            identityBefore.ShouldBeGreaterThan(lostClaimId);
            (await db.Set<Claim>().AnyAsync(c => c.ClaimId == lostClaimId)).ShouldBeFalse();
        }

        var lostClaim = new ClaimIdentification(projectId, lostClaimId);
        var shell = new RestoredClaimShell(lostClaim, characterId, playerId, masterId, CreatedAt);

        // 3. Вставка оболочки
        using (var scope = factory.Services.CreateScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            (await unitOfWork.GetCharacterAggregateWriteRepository().InsertRestoredClaimShell(shell)).ShouldBeTrue();
        }

        // 4. Строка на месте, с заданным id и своим обсуждением в том же проекте; счётчик не сдвинулся
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var claim = await db.Set<Claim>().AsNoTracking()
                .Include(c => c.CommentDiscussion)
                .SingleAsync(c => c.ClaimId == lostClaimId);

            claim.ProjectId.ShouldBe(projectId.Value);
            claim.CharacterId.ShouldBe(characterId.CharacterId);
            claim.PlayerUserId.ShouldBe(playerId.Value);
            claim.ResponsibleMasterUserId.ShouldBe(masterId.Value);
            claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByMaster);
            claim.CreateDate.ShouldBe(CreatedAt);
            claim.CommentDiscussion.ShouldNotBeNull();
            claim.CommentDiscussion.ProjectId.ShouldBe(projectId.Value);

            (await GetClaimsIdentityAsync(db)).ShouldBe(identityBefore);
        }

        // 5. Повторная вставка того же id ничего не делает
        using (var scope = factory.Services.CreateScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            (await unitOfWork.GetCharacterAggregateWriteRepository().InsertRestoredClaimShell(shell)).ShouldBeFalse();

            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            (await db.Set<Claim>().CountAsync(c => c.ClaimId == lostClaimId)).ShouldBe(1);
        }

        // 6. Оболочка — полноценная заявка: write-хэндл агрегата собирает по ней снимки
        using (var scope = factory.Services.CreateScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var handle = await unitOfWork.GetCharacterAggregateWriteRepository().LoadClaimForUpdate(lostClaim);

            handle.Claim.ClaimId.ShouldBe(lostClaimId);
            handle.CharacterClaimInfo.ClaimId.ShouldBe(lostClaim);
            handle.CharacterInfo.Id.ShouldBe(characterId);
        }

        // 7. Обычная заявка после восстановления получает следующий id счётчика, а не занятый
        var nextClaimId = await factory.Services.RunAsAsync(otherPlayerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                otherCharacterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });
        nextClaimId.ClaimId.ShouldBe(identityBefore + 1);
    }

    /// <summary>
    /// Расходует два значения IDENTITY таблицы заявок вставками, которые тут же откатываются, и
    /// возвращает первое из них. Переменная откат транзакции переживает.
    /// </summary>
    private static Task<int> BurnTwoClaimIdsAsync(
        MyDbContext db,
        ProjectIdentification projectId,
        CharacterIdentification characterId,
        UserIdentification playerId,
        UserIdentification masterId)
    {
        const string sql = """
            SET NOCOUNT ON;
            DECLARE @first int;
            BEGIN TRANSACTION;
            INSERT INTO dbo.CommentDiscussions (ProjectId) VALUES (@p0);
            INSERT INTO dbo.Claims (ProjectId, CharacterId, PlayerUserId, ResponsibleMasterUserId,
                ClaimStatus, CommentDiscussionId, CreateDate, PlayerAcceptedDate, LastUpdateDateTime,
                PreferentialFeeUser, PlayerAllowedSenstiveData)
            VALUES (@p0, @p1, @p2, @p3, 0, CAST(SCOPE_IDENTITY() AS int), @p4, @p4, @p4, 0, 0);
            SET @first = CAST(SCOPE_IDENTITY() AS int);
            INSERT INTO dbo.CommentDiscussions (ProjectId) VALUES (@p0);
            INSERT INTO dbo.Claims (ProjectId, CharacterId, PlayerUserId, ResponsibleMasterUserId,
                ClaimStatus, CommentDiscussionId, CreateDate, PlayerAcceptedDate, LastUpdateDateTime,
                PreferentialFeeUser, PlayerAllowedSenstiveData)
            VALUES (@p0, @p1, @p2, @p3, 0, CAST(SCOPE_IDENTITY() AS int), @p4, @p4, @p4, 0, 0);
            ROLLBACK TRANSACTION;
            SELECT @first;
            """;

        return db.Database.SqlQuery<int>(
                sql,
                projectId.Value,
                characterId.CharacterId,
                playerId.Value,
                masterId.Value,
                CreatedAt)
            .SingleAsync();
    }

    private static Task<int> GetClaimsIdentityAsync(MyDbContext db)
        => db.Database.SqlQuery<int>("SELECT CAST(IDENT_CURRENT('dbo.Claims') AS int)").SingleAsync();
}
