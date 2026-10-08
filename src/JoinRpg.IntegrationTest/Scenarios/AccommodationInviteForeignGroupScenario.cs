using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Приглашение группы проживающих, которой нет в плане поселения приглашающего: она другого типа,
/// у приглашающего типа нет вовсе, или такой группы в проекте нет.
/// </summary>
/// <remarks>
/// Такая цель разворачивается отдельной веткой (<c>LoadForeignGroup</c>): трекаемая группа из БД —
/// ради существования и типа, затем план поселения её типа (ADR022 §3). Ветка ходит в базу двумя
/// запросами с фильтром по проекту, поэтому проверяется на настоящем SQL Server: ошибка должна
/// быть внятной (<see cref="AccommodationInviteNotAllowedException"/>), а не 500-й, и ни одной строки
/// приглашения не должно появиться.
/// Сид свой: сценарий мутирует данные, а смоук-проект общий и только читается.
/// </remarks>
public class AccommodationInviteForeignGroupScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    /// <summary>
    /// Приглашающий типа A приглашает группу типа B (другая категория, другой план) — отказ про тип.
    /// </summary>
    [Fact]
    public async Task InviteGroupOfOtherType_IsRefusedByType()
    {
        var seed = await SeedAsync();

        var exception = await InviteAsync(seed, seed.InviterA, seed.InviterAGroup, seed.GroupB);

        exception.Message.ShouldContain("такой же тип проживания");
        (await CountInvitesAsync(seed.ProjectId)).ShouldBe(0);
    }

    /// <summary>
    /// Приглашающий без типа приглашает группу типа B — отказ «не выбран тип».
    /// </summary>
    [Fact]
    public async Task InviterWithoutType_InvitesGroup_IsRefusedAsNoType()
    {
        var seed = await SeedAsync();

        // Своей группы у приглашающего нет, поэтому ссылку на неё сервис сверять не с чем — передаём
        // заведомо несуществующую.
        var exception = await InviteAsync(
            seed,
            seed.InviterWithoutType,
            new AccommodationRequestIdentification(seed.ProjectId, int.MaxValue),
            seed.GroupB);

        exception.Message.ShouldContain("не выбран тип");
        (await CountInvitesAsync(seed.ProjectId)).ShouldBe(0);
    }

    /// <summary>
    /// Приглашение группы, которой нет в базе вовсе, — отказ «группа не найдена», а не 500.
    /// </summary>
    [Fact]
    public async Task InviteMissingGroup_IsRefusedAsNotFound()
    {
        var seed = await SeedAsync();

        var exception = await InviteAsync(
            seed,
            seed.InviterA,
            seed.InviterAGroup,
            new AccommodationRequestIdentification(seed.ProjectId, int.MaxValue));

        exception.Message.ShouldContain("не найдена");
        (await CountInvitesAsync(seed.ProjectId)).ShouldBe(0);
    }

    /// <summary>
    /// Группа из другого проекта, выданная за группу своего проекта (идентификатор подменён в
    /// запросе), — тоже «не найдена»: загрузка группы фильтрует по проекту.
    /// </summary>
    [Fact]
    public async Task InviteGroupOfOtherProject_IsRefusedAsNotFound()
    {
        var seed = await SeedAsync();
        var other = await SeedAsync();

        var exception = await InviteAsync(
            seed,
            seed.InviterA,
            seed.InviterAGroup,
            new AccommodationRequestIdentification(seed.ProjectId, other.GroupB.AccommodationRequestId));

        exception.Message.ShouldContain("не найдена");
        (await CountInvitesAsync(seed.ProjectId)).ShouldBe(0);
        (await CountInvitesAsync(other.ProjectId)).ShouldBe(0);
    }

    private Task<AccommodationInviteNotAllowedException> InviteAsync(
        Seed seed,
        ClaimIdentification sender,
        AccommodationRequestIdentification senderRequest,
        AccommodationRequestIdentification targetGroup)
        => factory.Services.RunAsAsync(seed.OwnerId, sp =>
            Should.ThrowAsync<AccommodationInviteNotAllowedException>(() =>
                sp.GetRequiredService<IAccommodationInviteService>().CreateAccommodationInvite(
                    sender,
                    senderRequest,
                    AccommodationGroupIdentification.From(targetGroup))));

    private async Task<int> CountInvitesAsync(ProjectIdentification projectId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<AccommodationInvite>()
            .CountAsync(invite => invite.ProjectId == projectId.Value);
    }

    /// <summary>
    /// Проект с поселением: типы A и B (каждый в своей категории — отдельный план поселения) и три
    /// заявки — в группе типа A, в группе типа B и без типа.
    /// </summary>
    private async Task<Seed> SeedAsync()
    {
        var (ownerId, _) = await CreateUserAsync();

        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, ownerId, "Проект для приглашения чужой группы");
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

        var typeA = await CreateTypeAsync(ownerId, projectId, "Домик", capacity: 4);
        var typeB = await CreateTypeAsync(ownerId, projectId, "Палатка", capacity: 4);

        // Предусловие сценария: типы селятся из разных категорий, то есть лежат в разных планах.
        await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var settings = (await sp.GetRequiredService<IProjectMetadataRepository>()
                .GetProjectMetadata(projectId)).AccommodationSettings;
            settings.GetTypeById(typeA).RoomCategoryId.ShouldNotBe(settings.GetTypeById(typeB).RoomCategoryId);
        });

        var claims = await CreateClaimsAsync(ownerId, projectId, count: 3);
        var (inviterA, inviteeB, withoutType) = (claims[0], claims[1], claims[2]);

        var groupIds = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            await claimService.SetAccommodationType(projectId.Value, inviterA.ClaimId, typeA.AccommodationTypeId);
            await claimService.SetAccommodationType(projectId.Value, inviteeB.ClaimId, typeB.AccommodationTypeId);

            return await AccommodationTestHelpers.GetAccommodationGroupIdsAsync(sp, [inviterA, inviteeB]);
        });

        return new Seed(ownerId, projectId, inviterA, groupIds[0], withoutType, groupIds[1]);
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
        ClaimIdentification InviterA,
        AccommodationRequestIdentification InviterAGroup,
        ClaimIdentification InviterWithoutType,
        AccommodationRequestIdentification GroupB);
}
