using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Смена группы проживающих операциями заявки (<see cref="IClaimService"/>): выбор другого типа,
/// выход из группы и отклонение заявки — на настоящей БД.
/// </summary>
/// <remarks>
/// Все три операции за одно сохранение делают несколько связанных правок: обнуляют у заявки
/// <c>AccommodationRequest_Id</c> вместе с навигацией, удаляют опустевшую группу и создают новую,
/// привязывая к ней заявку коллекцией <c>Subjects</c> (у новой группы до сохранения нет <c>Id</c>).
/// Порядок вставок и удалений и согласованность «ключ против ссылки» разбирает EF6 на реальных
/// внешних ключах — юнит-тест на фейках этого не видит (ADR022, PR 6).
/// Сид свой, не смоучный: сценарий мутирует данные, а смоук-проект общий и только читается.
/// </remarks>
public class ClaimAccommodationGroupChangeScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    /// <summary>
    /// Заявка одна в своей группе выбирает другой тип: старая группа удалена, создана новая
    /// нужного типа, внешний ключ заявки и доменный снимок указывают на неё.
    /// </summary>
    [Fact]
    public async Task SetAccommodationType_AloneInGroup_ReplacesGroup()
    {
        var seed = await SeedAsync(claimCount: 1);
        var claimId = seed.Claims[0];
        var oldGroupId = seed.GroupIds[0];

        await factory.Services.RunAsAsync(seed.OwnerId, sp =>
            sp.GetRequiredService<IClaimService>().SetAccommodationType(
                seed.ProjectId.Value, claimId.ClaimId, seed.TypeB.AccommodationTypeId));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            (await db.Set<AccommodationRequest>().AnyAsync(r => r.Id == oldGroupId.AccommodationRequestId))
                .ShouldBeFalse("Опустевшая старая группа должна быть удалена");

            var newGroupId = await GetGroupFkAsync(db, claimId);
            newGroupId.ShouldNotBeNull("Заявка должна попасть в новую группу");
            newGroupId.ShouldNotBe(oldGroupId.AccommodationRequestId);

            var newGroup = await db.Set<AccommodationRequest>().SingleAsync(r => r.Id == newGroupId);
            newGroup.AccommodationTypeId.ShouldBe(seed.TypeB.AccommodationTypeId);
            newGroup.ProjectId.ShouldBe(seed.ProjectId.Value);
            newGroup.AccommodationId.ShouldBeNull();
            newGroup.IsAccepted.ShouldBe(InviteState.Accepted);
            (await GetSubjectsAsync(db, newGroupId.Value)).ShouldBe([claimId.ClaimId]);

            (await db.Set<AccommodationRequest>().CountAsync(r => r.ProjectId == seed.ProjectId.Value))
                .ShouldBe(1, "В проекте должна остаться ровно одна группа — новая");
        }

        var snapshot = await GetClaimInfoAsync(seed.OwnerId, claimId);
        snapshot.AccommodationTypeId.ShouldBe(seed.TypeB);
        snapshot.AccommodationGroupId.AsAccommodationRequestId().ShouldNotBe(oldGroupId);
    }

    /// <summary>
    /// Из группы двоих (сложенной приёмом приглашения) один уходит: у него новая группа того же типа,
    /// у оставшегося — прежняя группа, и в ней ровно он.
    /// </summary>
    [Fact]
    public async Task LeaveAccommodationGroup_FromGroupOfTwo_SplitsGroup()
    {
        var seed = await SeedAsync(claimCount: 2);
        var sharedGroupId = await JoinGroupAsync(seed);
        var stayer = seed.Claims[0];
        var leaver = seed.Claims[1];

        await factory.Services.RunAsAsync(seed.OwnerId, sp =>
            sp.GetRequiredService<IClaimService>().LeaveAccommodationGroupAsync(
                seed.ProjectId.Value, leaver.ClaimId));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            (await GetGroupFkAsync(db, stayer)).ShouldBe(sharedGroupId);
            (await GetSubjectsAsync(db, sharedGroupId)).ShouldBe(
                [stayer.ClaimId],
                "В прежней группе должен остаться только оставшийся");

            var leaverGroupId = await GetGroupFkAsync(db, leaver);
            leaverGroupId.ShouldNotBeNull("Ушедший должен получить свою группу");
            leaverGroupId.ShouldNotBe(sharedGroupId);

            var leaverGroup = await db.Set<AccommodationRequest>().SingleAsync(r => r.Id == leaverGroupId);
            leaverGroup.AccommodationTypeId.ShouldBe(seed.TypeA.AccommodationTypeId, "Тип у новой группы тот же");
            leaverGroup.IsAccepted.ShouldBe(InviteState.Accepted);
            (await GetSubjectsAsync(db, leaverGroupId.Value)).ShouldBe([leaver.ClaimId]);
        }

        var leaverSnapshot = await GetClaimInfoAsync(seed.OwnerId, leaver);
        leaverSnapshot.AccommodationTypeId.ShouldBe(seed.TypeA);
        leaverSnapshot.AccommodationGroupId.AsAccommodationRequestId()?.AccommodationRequestId
            .ShouldNotBe(sharedGroupId);
    }

    /// <summary>
    /// Отклонение заявки из расселённой группы двоих: заявка выходит из группы, группа с оставшимся
    /// живёт в той же комнате. Отклонение последнего жильца удаляет группу.
    /// </summary>
    [Fact]
    public async Task DeclineByMaster_FromPlacedGroupOfTwo_LeavesGroupInRoom_ThenLastDeletesGroup()
    {
        var seed = await SeedAsync(claimCount: 2);
        var sharedGroupId = await JoinGroupAsync(seed);
        var stayer = seed.Claims[0];
        var declined = seed.Claims[1];

        await factory.Services.RunAsAsync(seed.OwnerId, sp =>
            sp.GetRequiredService<IAccommodationService>().OccupyRoom(
                seed.RoomId,
                [new AccommodationRequestIdentification(seed.ProjectId, sharedGroupId)]));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            (await db.Set<AccommodationRequest>().SingleAsync(r => r.Id == sharedGroupId))
                .AccommodationId.ShouldBe(seed.RoomId.RoomId, "Предусловие: группа заселена");
        }

        await factory.Services.RunAsAsync(seed.OwnerId, sp =>
            sp.GetRequiredService<IClaimService>().DeclineByMaster(
                declined, ClaimDenialReason.NotSuitable, "Не подошла", deleteCharacter: false));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            (await GetGroupFkAsync(db, declined)).ShouldBeNull("Отклонённая заявка должна выйти из группы");

            var group = await db.Set<AccommodationRequest>().SingleAsync(r => r.Id == sharedGroupId);
            group.AccommodationId.ShouldBe(seed.RoomId.RoomId, "Оставшийся живёт в той же комнате");
            (await GetSubjectsAsync(db, sharedGroupId)).ShouldBe([stayer.ClaimId]);
        }

        await factory.Services.RunAsAsync(seed.OwnerId, sp =>
            sp.GetRequiredService<IClaimService>().DeclineByMaster(
                stayer, ClaimDenialReason.NotSuitable, "Тоже не подошла", deleteCharacter: false));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            (await GetGroupFkAsync(db, stayer)).ShouldBeNull("Последний жилец должен выйти из группы");
            (await db.Set<AccommodationRequest>().AnyAsync(r => r.Id == sharedGroupId))
                .ShouldBeFalse("Опустевшая группа должна быть удалена");
            (await db.Set<AccommodationRequest>().AnyAsync(r => r.ProjectId == seed.ProjectId.Value))
                .ShouldBeFalse("Других групп в проекте не появилось");
        }
    }

    private static Task<int?> GetGroupFkAsync(MyDbContext db, ClaimIdentification claimId)
        => db.Set<Claim>()
            .Where(claim => claim.ClaimId == claimId.ClaimId)
            .Select(claim => claim.AccommodationRequest_Id)
            .SingleAsync();

    private static Task<List<int>> GetSubjectsAsync(MyDbContext db, int groupId)
        => db.Set<Claim>()
            .Where(claim => claim.AccommodationRequest_Id == groupId)
            .Select(claim => claim.ClaimId)
            .OrderBy(id => id)
            .ToListAsync();

    private Task<CharacterClaimInfo> GetClaimInfoAsync(UserIdentification ownerId, ClaimIdentification claimId)
        => factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var characters = await sp.GetRequiredService<ICharacterInfoRepository>()
                .GetCharacterInfosByClaims([claimId]);
            return characters.SelectMany(character => character.Claims).Single(claim => claim.ClaimId == claimId);
        });

    /// <summary>
    /// Складывает группу из первых двух заявок сида: первая приглашает вторую, вторая принимает.
    /// Возвращает группу, в которой они оказались.
    /// </summary>
    private async Task<int> JoinGroupAsync(Seed seed)
    {
        var sender = seed.Claims[0];
        var receiver = seed.Claims[1];

        await factory.Services.RunAsAsync(seed.OwnerId, sp =>
            sp.GetRequiredService<IAccommodationInviteService>().CreateAccommodationInvite(
                sender,
                seed.GroupIds[0],
                AccommodationGroupIdentification.From(receiver)));

        int inviteId;
        using (var scope = factory.Services.CreateScope())
        {
            inviteId = await scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<AccommodationInvite>()
                .Where(invite => invite.FromClaimId == sender.ClaimId && invite.ToClaimId == receiver.ClaimId)
                .Select(invite => invite.Id)
                .SingleAsync();
        }

        await factory.Services.RunAsAsync(seed.OwnerId, sp =>
            sp.GetRequiredService<IAccommodationInviteService>().AcceptAccommodationInvite(
                new AccommodationInviteIdentification(seed.ProjectId, inviteId)));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var senderGroup = await GetGroupFkAsync(db, sender);
            senderGroup.ShouldNotBeNull();
            (await GetGroupFkAsync(db, receiver)).ShouldBe(senderGroup, "Предусловие: заявки съехались");
            (await GetSubjectsAsync(db, senderGroup.Value)).Count.ShouldBe(2);
            return senderGroup.Value;
        }
    }

    /// <summary>
    /// Проект с поселением: тип A на двоих с одной комнатой, тип B на одного, и
    /// <paramref name="claimCount"/> заявок, каждая в своей одноместной группе типа A.
    /// </summary>
    private async Task<Seed> SeedAsync(int claimCount)
    {
        var (ownerId, _) = await CreateUserAsync();

        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, ownerId, "Проект для смены группы проживающих");
        }

        await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                .GetProjectMetadata(projectId);

            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
            await projectService.SetAccommodationSettings(projectId, enableAccommodation: true);
        });

        var typeA = await CreateTypeAsync(ownerId, projectId, "Двушка", capacity: 2);
        var typeB = await CreateTypeAsync(ownerId, projectId, "Одиночка", capacity: 1);

        var roomId = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                .GetProjectMetadata(projectId);
            var rooms = await sp.GetRequiredService<IAccommodationService>().AddRooms(
                projectInfo.AccommodationSettings.GetTypeById(typeA).RoomCategoryId,
                ["1"]);
            return rooms.Single();
        });

        var claims = await CreateClaimsAsync(ownerId, projectId, claimCount);

        var groupIds = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            foreach (var claimId in claims)
            {
                await claimService.SetAccommodationType(
                    projectId.Value, claimId.ClaimId, typeA.AccommodationTypeId);
            }

            return await AccommodationTestHelpers.GetAccommodationGroupIdsAsync(sp, claims);
        });

        return new Seed(ownerId, projectId, typeA, typeB, roomId, claims, groupIds);
    }

    private Task<AccommodationTypeIdentification> CreateTypeAsync(
        UserIdentification ownerId,
        ProjectIdentification projectId,
        string name,
        int capacity)
        => factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IAccommodationTypeService>().CreateAccommodationType(
                projectId,
                new AccommodationTypeRequest(
                    name,
                    new MarkdownString(name),
                    Cost: 0,
                    Capacity: capacity,
                    IsPlayerSelectable: true)));

    /// <summary>Заявки игроков на новых персонажей — по одному игроку на заявку.</summary>
    private async Task<IReadOnlyList<ClaimIdentification>> CreateClaimsAsync(
        UserIdentification ownerId,
        ProjectIdentification projectId,
        int count)
    {
        var characters = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                .GetProjectMetadata(projectId);
            var characterService = sp.GetRequiredService<ICharacterService>();
            var result = new List<CharacterIdentification>(count);
            for (var i = 0; i < count; i++)
            {
                result.Add(await characterService.AddCharacter(new AddCharacterRequest(
                    projectId,
                    ParentCharacterGroupIds: [],
                    new CharacterTypeInfo(
                        CharacterType.Player,
                        IsHot: false,
                        SlotLimit: null,
                        SlotName: null,
                        CharacterVisibility.Public),
                    FieldValues: FieldLayerContainer.Empty(projectInfo))));
            }

            return result;
        });

        var claims = new List<ClaimIdentification>(count);
        foreach (var characterId in characters)
        {
            var (playerId, _) = await CreateUserAsync();
            claims.Add(await factory.Services.RunAsAsync(playerId, async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                    .GetProjectMetadata(projectId);
                return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                    characterId,
                    "Хочу играть эту роль",
                    FieldLayerContainer.Empty(projectInfo),
                    sensitiveDataAllowed: true);
            }));
        }

        return claims;
    }

    private async Task<(UserIdentification UserId, string Email)> CreateUserAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
    }

    /// <summary>Проект с поселением, поднятый под один тест.</summary>
    private sealed record Seed(
        UserIdentification OwnerId,
        ProjectIdentification ProjectId,
        AccommodationTypeIdentification TypeA,
        AccommodationTypeIdentification TypeB,
        AccommodationRoomIdentification RoomId,
        IReadOnlyList<ClaimIdentification> Claims,
        IReadOnlyList<AccommodationRequestIdentification> GroupIds);
}
