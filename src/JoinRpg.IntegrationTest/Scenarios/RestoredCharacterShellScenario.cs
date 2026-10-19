using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Drp;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Восстановление потерянных группы и персонажа с прежними идентификаторами из выгрузки мастера
/// (DRP, #5529) на настоящем SQL Server: <c>SET IDENTITY_INSERT</c>, счётчики IDENTITY, которые явная
/// вставка ниже текущего значения двигать не должна, и паритет с доменным созданием — оболочка плюс
/// наполнение доменом дают те же строки, что обычное создание группы и персонажа.
/// </summary>
public class RestoredCharacterShellScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    private const string CharacterName = "Вася";
    private const string FieldValue = "Север";

    [Fact]
    public async Task RestoreLostGroupAndCharacter_InsertsWithGivenIds_LikeDomainCreation()
    {
        // 1. Мастер, проект (ларп: с полем-именем персонажа), строковое поле персонажа
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
        }

        var fieldId = await factory.Services.RunAsAsync(masterId, sp => AddStringField(sp, projectId, "Сторона"));

        // 2. Эталон — доменное создание: скрытая группа под корнем и скрытый персонаж игрока в ней,
        // с тем же именем и значением поля, что будут восстановлены.
        var (referenceGroupId, referenceCharacterId) = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            var groupId = await sp.GetRequiredService<ICharacterGroupService>().AddCharacterGroup(
                projectId, "Эталонная группа", isPublic: false, [projectInfo.GroupTree.RootGroupId], description: "");

            var nameField = projectInfo.CharacterNameField.ShouldNotBeNull();
            var characterId = await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                [groupId],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Private),
                new FieldLayerContainer(projectInfo, new Dictionary<int, string?>
                {
                    [nameField.Id.ProjectFieldId] = CharacterName,
                    [fieldId.ProjectFieldId] = FieldValue,
                })));
            return (groupId, characterId);
        });

        // 3. Дырки в счётчиках: так в бою выглядят id, выданные после бэкапа, — счётчики сдвинуты
        // вперёд (#5460), а самих строк нет.
        int lostGroupIntId;
        int lostCharacterIntId;
        int groupIdentityBefore;
        int characterIdentityBefore;
        RootGroupRow rootBefore;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            lostGroupIntId = await BurnTwoIdsAsync(db, BurnGroupsSql, projectId, masterId);
            lostCharacterIntId = await BurnTwoIdsAsync(db, BurnCharactersSql, projectId, masterId);
            groupIdentityBefore = await GetIdentityAsync(db, "CharacterGroups");
            characterIdentityBefore = await GetIdentityAsync(db, "Characters");
            rootBefore = await GetRootGroupAsync(db, projectId);

            groupIdentityBefore.ShouldBeGreaterThan(lostGroupIntId);
            characterIdentityBefore.ShouldBeGreaterThan(lostCharacterIntId);
        }

        await BlockProject(projectId);

        var lostGroupId = new CharacterGroupIdentification(projectId, lostGroupIntId);
        var lostCharacterId = new CharacterIdentification(projectId, lostCharacterIntId);
        var target = new RestoredCharacterTarget(
            [lostGroupId],
            new Dictionary<ProjectFieldIdentification, string?> { [fieldId] = FieldValue });

        // 4. Восстановление — группа и персонаж в одном скоупе: персонаж ссылается на только что
        // вставленную группу, и кеш метаданных запроса должен её уже видеть.
        var before = DateTime.UtcNow.AddSeconds(-1);
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var service = sp.GetRequiredService<IExportRestoreService>();
            await service.RestoreLostGroup(lostGroupId, "Потерянная группа");
            await service.RestoreLostCharacter(lostCharacterId, CharacterName, CharacterType.Player, isHot: false, target);
        });

        // 5. Строки на месте с заданными id, как у доменного создания; счётчики не сдвинулись
        DateTime characterUpdatedAt;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            var restoredGroup = await GetGroupAsync(db, lostGroupIntId);
            restoredGroup.ProjectId.ShouldBe(projectId.Value);
            restoredGroup.CharacterGroupName.ShouldBe("Потерянная группа");
            restoredGroup.CreatedById.ShouldBe(masterId.Value);
            restoredGroup.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
            var referenceGroup = await GetGroupAsync(db, referenceGroupId.CharacterGroupId);
            Comparable(restoredGroup).ShouldBe(Comparable(referenceGroup));

            var restoredCharacter = await GetCharacterAsync(db, lostCharacterIntId);
            restoredCharacter.ProjectId.ShouldBe(projectId.Value);
            restoredCharacter.ParentGroupsImpl_ListIds.ShouldBe(lostGroupIntId.ToString());
            restoredCharacter.CreatedById.ShouldBe(masterId.Value);
            // Дата обновления проекта считается в том числе по UpdatedAt групп и персонажей.
            restoredCharacter.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
            characterUpdatedAt = restoredCharacter.UpdatedAt;
            var referenceCharacter = await GetCharacterAsync(db, referenceCharacterId.CharacterId);
            referenceCharacter.ParentGroupsImpl_ListIds.ShouldBe(referenceGroupId.CharacterGroupId.ToString());
            Comparable(restoredCharacter).ShouldBe(Comparable(referenceCharacter));

            // Порядок детей корневой группы не тронут — как и при доменном создании.
            (await GetRootGroupAsync(db, projectId)).ShouldBe(rootBefore);

            (await GetIdentityAsync(db, "CharacterGroups")).ShouldBe(groupIdentityBefore);
            (await GetIdentityAsync(db, "Characters")).ShouldBe(characterIdentityBefore);
        }

        // 6. Повтор — ничего не меняет; вставка оболочек того же id — «уже есть»
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var service = sp.GetRequiredService<IExportRestoreService>();
            await service.RestoreLostGroup(lostGroupId, "Потерянная группа");
            await service.RestoreLostCharacter(lostCharacterId, CharacterName, CharacterType.Player, isHot: false, target);

            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            var root = projectInfo.GroupTree.RootGroupId;
            (await unitOfWork.GetProjectMetadataWriteRepository().InsertRestoredCharacterGroupShell(
                new RestoredCharacterGroupShell(lostGroupId, root, "Другое", masterId, DateTime.UtcNow)))
                .ShouldBe(RestoredShellInsertResult.AlreadyExists);
            (await unitOfWork.GetCharacterAggregateWriteRepository().InsertRestoredCharacterShell(
                new RestoredCharacterShell(lostCharacterId, root, "Другой", null, masterId, DateTime.UtcNow, CharacterType.Player, IsHot: false)))
                .ShouldBe(RestoredShellInsertResult.AlreadyExists);
        });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            (await GetCharacterAsync(db, lostCharacterIntId)).UpdatedAt.ShouldBe(characterUpdatedAt);
            (await db.Set<DataModel.CharacterGroup>().CountAsync(g => g.CharacterGroupId == lostGroupIntId)).ShouldBe(1);
            (await db.Set<DataModel.Character>().CountAsync(c => c.CharacterId == lostCharacterIntId)).ShouldBe(1);
        }

        // 7. Восстановленный персонаж — полноценный: агрегат загружается, группа — в дереве
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var handle = await sp.GetRequiredService<IUnitOfWork>().GetCharacterAggregateWriteRepository()
                .LoadCharacterForUpdate(lostCharacterId);
            handle.CharacterInfo.Id.ShouldBe(lostCharacterId);
            handle.ProjectInfo.GroupTree.Contains(lostGroupId).ShouldBeTrue();
        });

        // 8. Id, занятый в другом проекте, и id выше счётчика — понятный отказ, счётчики не сдвинуты
        ProjectIdentification otherProjectId;
        using (var scope = factory.Services.CreateScope())
        {
            otherProjectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Другой проект");
        }
        await BlockProject(otherProjectId);

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var service = sp.GetRequiredService<IExportRestoreService>();
            _ = await Should.ThrowAsync<JoinValidationException>(
                () => service.RestoreLostGroup(new CharacterGroupIdentification(otherProjectId, lostGroupIntId), "Чужая"));
            _ = await Should.ThrowAsync<JoinValidationException>(
                () => service.RestoreLostCharacter(
                    new CharacterIdentification(otherProjectId, lostCharacterIntId), "Чужой", CharacterType.Player, isHot: false, new RestoredCharacterTarget(null, null)));
            _ = await Should.ThrowAsync<JoinValidationException>(
                () => service.RestoreLostGroup(new CharacterGroupIdentification(otherProjectId, groupIdentityBefore + 1000), "Будущая"));
            _ = await Should.ThrowAsync<JoinValidationException>(
                () => service.RestoreLostCharacter(
                    new CharacterIdentification(otherProjectId, characterIdentityBefore + 1000), "Будущий", CharacterType.Player, isHot: false, new RestoredCharacterTarget(null, null)));
        });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            (await GetGroupAsync(db, lostGroupIntId)).ProjectId.ShouldBe(projectId.Value);
            (await GetCharacterAsync(db, lostCharacterIntId)).ProjectId.ShouldBe(projectId.Value);
            (await db.Set<DataModel.CharacterGroup>().AnyAsync(g => g.CharacterGroupId == groupIdentityBefore + 1000)).ShouldBeFalse();
            (await db.Set<DataModel.Character>().AnyAsync(c => c.CharacterId == characterIdentityBefore + 1000)).ShouldBeFalse();
        }

        // 9. То же на уровне репозиториев: занятый другим проектом id, id выше счётчика и граница —
        // id, равный текущему значению счётчика (выданный, но откаченный), — тоже отказ.
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var db = sp.GetRequiredService<MyDbContext>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var groups = unitOfWork.GetProjectMetadataWriteRepository();
            var characters = unitOfWork.GetCharacterAggregateWriteRepository();
            var otherRoot = (await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(otherProjectId))
                .GroupTree.RootGroupId;

            Task<RestoredShellInsertResult> InsertGroup(int id)
                => groups.InsertRestoredCharacterGroupShell(new RestoredCharacterGroupShell(
                    new CharacterGroupIdentification(otherProjectId, id), otherRoot, "Группа", masterId, DateTime.UtcNow));
            Task<RestoredShellInsertResult> InsertCharacter(int id)
                => characters.InsertRestoredCharacterShell(new RestoredCharacterShell(
                    new CharacterIdentification(otherProjectId, id), otherRoot, "Персонаж", null, masterId, DateTime.UtcNow, CharacterType.Player, IsHot: false));

            (await InsertGroup(lostGroupIntId)).ShouldBe(RestoredShellInsertResult.TakenInOtherProject);
            (await InsertCharacter(lostCharacterIntId)).ShouldBe(RestoredShellInsertResult.TakenInOtherProject);

            _ = await BurnTwoIdsAsync(db, BurnGroupsSql, otherProjectId, masterId);
            _ = await BurnTwoIdsAsync(db, BurnCharactersSql, otherProjectId, masterId);
            var groupIdentity = await GetIdentityAsync(db, "CharacterGroups");
            var characterIdentity = await GetIdentityAsync(db, "Characters");
            (await db.Set<DataModel.CharacterGroup>().AnyAsync(g => g.CharacterGroupId == groupIdentity)).ShouldBeFalse();
            (await db.Set<DataModel.Character>().AnyAsync(c => c.CharacterId == characterIdentity)).ShouldBeFalse();

            (await InsertGroup(groupIdentity)).ShouldBe(RestoredShellInsertResult.NotLostId);
            (await InsertCharacter(characterIdentity)).ShouldBe(RestoredShellInsertResult.NotLostId);
            (await InsertGroup(groupIdentity + 1000)).ShouldBe(RestoredShellInsertResult.NotLostId);
            (await InsertCharacter(characterIdentity + 1000)).ShouldBe(RestoredShellInsertResult.NotLostId);

            (await db.Set<DataModel.CharacterGroup>().AnyAsync(g => g.CharacterGroupId == groupIdentity)).ShouldBeFalse();
            (await db.Set<DataModel.Character>().AnyAsync(c => c.CharacterId == characterIdentity)).ShouldBeFalse();
            (await GetIdentityAsync(db, "CharacterGroups")).ShouldBe(groupIdentity);
            (await GetIdentityAsync(db, "Characters")).ShouldBe(characterIdentity);
        });
    }

    /// <summary>
    /// Тип из «Занят?» выгрузки (#5645): шаблон, горячий шаблон, NPC, горячая вакансия восстанавливаются
    /// такими же строками, как их доменное создание (тип, «горячесть», приём заявок, лимит слота).
    /// </summary>
    [Theory]
    [InlineData(CharacterType.Slot, false)]
    [InlineData(CharacterType.Slot, true)]
    [InlineData(CharacterType.NonPlayer, false)]
    [InlineData(CharacterType.Player, true)]
    public async Task RestoreLostCharacter_OfType_LikeDomainCreation(CharacterType type, bool isHot)
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
        }

        var fieldId = await factory.Services.RunAsAsync(masterId, sp => AddStringField(sp, projectId, "Сторона"));

        // Эталон — доменное создание того же типа в корневой группе.
        var referenceCharacterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            var nameField = projectInfo.CharacterNameField.ShouldNotBeNull();
            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                [projectInfo.GroupTree.RootGroupId],
                new CharacterTypeInfo(
                    type,
                    isHot,
                    SlotLimit: null,
                    SlotName: type == CharacterType.Slot ? CharacterName : null,
                    CharacterVisibility.Private),
                new FieldLayerContainer(projectInfo, new Dictionary<int, string?>
                {
                    [nameField.Id.ProjectFieldId] = CharacterName,
                    [fieldId.ProjectFieldId] = FieldValue,
                })));
        });

        int lostCharacterIntId;
        using (var scope = factory.Services.CreateScope())
        {
            lostCharacterIntId = await BurnTwoIdsAsync(scope.ServiceProvider.GetRequiredService<MyDbContext>(), BurnCharactersSql, projectId, masterId);
        }
        await BlockProject(projectId);

        var lostCharacterId = new CharacterIdentification(projectId, lostCharacterIntId);
        await factory.Services.RunAsAsync(masterId, sp => sp.GetRequiredService<IExportRestoreService>().RestoreLostCharacter(
            lostCharacterId,
            CharacterName,
            type,
            isHot,
            new RestoredCharacterTarget(null, new Dictionary<ProjectFieldIdentification, string?> { [fieldId] = FieldValue })));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var restored = await GetCharacterAsync(db, lostCharacterIntId);
            restored.CharacterType.ShouldBe((int)type);
            restored.IsHot.ShouldBe(isHot);
            restored.IsAcceptingClaims.ShouldBe(type != CharacterType.NonPlayer);
            restored.CharacterSlotLimit.ShouldBeNull();
            var reference = await GetCharacterAsync(db, referenceCharacterId.CharacterId);
            restored.ParentGroupsImpl_ListIds.ShouldBe(reference.ParentGroupsImpl_ListIds);
            Comparable(restored).ShouldBe(Comparable(reference));
        }
    }

    private static Task<ProjectFieldIdentification> AddStringField(IServiceProvider sp, ProjectIdentification projectId, string name)
        => sp.GetRequiredService<IFieldSetupService>().AddField(new CreateFieldRequest(
            projectId,
            ProjectFieldType.String,
            name,
            fieldHint: "",
            canPlayerEdit: false,
            canPlayerView: true,
            isPublic: false,
            FieldBoundTo.Character,
            MandatoryStatus.Optional,
            showForGroups: [],
            validForNpc: true,
            includeInPrint: false,
            showForUnapprovedClaims: true,
            price: 0,
            masterFieldHint: "",
            programmaticValue: null));

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

    private const string BurnGroupsSql = """
        INSERT INTO dbo.CharacterGroups (ProjectId, IsRoot, IsPublic, IsActive, CreatedAt, CreatedById, UpdatedAt, UpdatedById)
        VALUES (@p0, 0, 0, 1, @p2, @p1, @p2, @p1);
        """;

    private const string BurnCharactersSql = """
        INSERT INTO dbo.Characters (ProjectId, IsPublic, IsAcceptingClaims, IsActive, CreatedAt, CreatedById, UpdatedAt, UpdatedById)
        VALUES (@p0, 0, 1, 1, @p2, @p1, @p2, @p1);
        """;

    /// <summary>
    /// Расходует два значения IDENTITY таблицы вставками, которые тут же откатываются, и возвращает
    /// первое из них — оно строго ниже счётчика. Переменная откат транзакции переживает.
    /// </summary>
    private static Task<int> BurnTwoIdsAsync(MyDbContext db, string insertSql, ProjectIdentification projectId, UserIdentification userId)
    {
        var sql = $"""
            SET NOCOUNT ON;
            DECLARE @first int;
            BEGIN TRANSACTION;
            {insertSql}
            SET @first = CAST(SCOPE_IDENTITY() AS int);
            {insertSql}
            ROLLBACK TRANSACTION;
            SELECT @first;
            """;

        return db.Database.SqlQuery<int>(sql, projectId.Value, userId.Value, DateTime.UtcNow).SingleAsync();
    }

    private static Task<int> GetIdentityAsync(MyDbContext db, string table)
        => db.Database.SqlQuery<int>($"SELECT CAST(IDENT_CURRENT('dbo.{table}') AS int)").SingleAsync();

    private static Task<GroupRow> GetGroupAsync(MyDbContext db, int groupId)
        => db.Database.SqlQuery<GroupRow>(
                """
                SELECT ProjectId, CharacterGroupName, IsRoot, IsSpecial, IsPublic, IsActive,
                    Description_Contents AS Description, ParentGroupsImpl_ListIds, ResponsibleMasterUserId,
                    ChildCharactersOrdering, ChildGroupsOrdering, CreatedById, UpdatedById, UpdatedAt
                FROM dbo.CharacterGroups WHERE CharacterGroupId = @p0
                """,
                groupId)
            .SingleAsync();

    private static Task<CharacterRow> GetCharacterAsync(MyDbContext db, int characterId)
        => db.Database.SqlQuery<CharacterRow>(
                """
                SELECT ProjectId, CharacterName, CharacterType, IsPublic, HidePlayerForCharacter, IsAcceptingClaims,
                    IsHot, CharacterSlotLimit, IsActive, InGame, AutoCreated, JsonData,
                    Description_Contents AS Description, ParentGroupsImpl_ListIds, PlotElementOrderData,
                    ApprovedClaimId, OriginalCharacterSlot_CharacterId AS OriginalCharacterSlotId,
                    CreatedById, UpdatedById, UpdatedAt
                FROM dbo.Characters WHERE CharacterId = @p0
                """,
                characterId)
            .SingleAsync();

    private static Task<RootGroupRow> GetRootGroupAsync(MyDbContext db, ProjectIdentification projectId)
        => db.Database.SqlQuery<RootGroupRow>(
                """
                SELECT ChildCharactersOrdering, ChildGroupsOrdering, UpdatedAt
                FROM dbo.CharacterGroups WHERE ProjectId = @p0 AND IsRoot = 1
                """,
                projectId.Value)
            .SingleAsync();

    /// <summary>Значимые колонки группы — без id, имени, родителя-сущности и дат.</summary>
    private static object Comparable(GroupRow row) => new
    {
        row.IsRoot,
        row.IsSpecial,
        row.IsPublic,
        row.IsActive,
        row.Description,
        row.ResponsibleMasterUserId,
        row.ChildCharactersOrdering,
        row.ChildGroupsOrdering,
        row.CreatedById,
        row.UpdatedById,
        // Обе — прямо под корнем: родитель одинаковый.
        row.ParentGroupsImpl_ListIds,
    };

    /// <summary>Значимые колонки персонажа — без id, групп (у эталона своя) и дат.</summary>
    private static object Comparable(CharacterRow row) => new
    {
        row.CharacterName,
        row.CharacterType,
        row.IsPublic,
        row.HidePlayerForCharacter,
        row.IsAcceptingClaims,
        row.IsHot,
        row.CharacterSlotLimit,
        row.IsActive,
        row.InGame,
        row.AutoCreated,
        row.JsonData,
        row.Description,
        row.PlotElementOrderData,
        row.ApprovedClaimId,
        row.OriginalCharacterSlotId,
        row.CreatedById,
        row.UpdatedById,
    };

    private sealed class GroupRow
    {
        public int ProjectId { get; set; }
        public string? CharacterGroupName { get; set; }
        public bool IsRoot { get; set; }
        public bool IsSpecial { get; set; }
        public bool IsPublic { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public string? ParentGroupsImpl_ListIds { get; set; }
        public int? ResponsibleMasterUserId { get; set; }
        public string? ChildCharactersOrdering { get; set; }
        public string? ChildGroupsOrdering { get; set; }
        public int CreatedById { get; set; }
        public int UpdatedById { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class CharacterRow
    {
        public int ProjectId { get; set; }
        public string? CharacterName { get; set; }
        public int CharacterType { get; set; }
        public bool IsPublic { get; set; }
        public bool HidePlayerForCharacter { get; set; }
        public bool IsAcceptingClaims { get; set; }
        public bool IsHot { get; set; }
        public int? CharacterSlotLimit { get; set; }
        public bool IsActive { get; set; }
        public bool InGame { get; set; }
        public bool AutoCreated { get; set; }
        public string? JsonData { get; set; }
        public string? Description { get; set; }
        public string? ParentGroupsImpl_ListIds { get; set; }
        public string? PlotElementOrderData { get; set; }
        public int? ApprovedClaimId { get; set; }
        public int? OriginalCharacterSlotId { get; set; }
        public int CreatedById { get; set; }
        public int UpdatedById { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed record RootGroupRow
    {
        public string? ChildCharactersOrdering { get; set; }
        public string? ChildGroupsOrdering { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
