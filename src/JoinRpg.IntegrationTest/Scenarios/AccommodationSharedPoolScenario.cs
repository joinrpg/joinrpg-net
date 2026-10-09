using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Accommodation.Rooms;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Два типа проживания из одной категории комнат на настоящей БД (ADR020): «Люкс» на двоих и
/// «Люкс на одного» селятся из общих комнат.
/// </summary>
/// <remarks>
/// Юнит-тесты сервисов идут на фейках — запросы загрузчика и write-репозитория плана к общему пулу
/// (группы через типы категории, план по группе) они на SQL Server не проверяют. Здесь проверяется
/// именно это, плюс то, что страницы поселения с общим пулом открываются.
/// </remarks>
public class AccommodationSharedPoolScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task SiblingTypes_ShareRooms_AndLeaveThemOnDelete()
    {
        var seed = await SeedAsync();

        // Пара «Люкса» — в 101, одиночка «на одного» — в 102: 102 теперь полна.
        await factory.Services.RunAsAsync(seed.OwnerId, async sp =>
        {
            var service = sp.GetRequiredService<IAccommodationService>();
            await service.OccupyRoom(seed.Rooms[0], [seed.LuxGroup]);
            await service.OccupyRoom(seed.Rooms[1], [seed.SingleGroup]);
        });

        var plan = await LoadPlanAsync(seed.SingleTypeId);
        plan.AccommodationTypes.Select(type => type.Id).ShouldBe([seed.LuxTypeId, seed.SingleTypeId], ignoreOrder: true);
        plan.Rooms.Count.ShouldBe(2);
        plan.Groups.Count.ShouldBe(2);
        plan.RoomCapacity.ShouldBe(2);
        plan.IsFull(seed.Rooms[1]).ShouldBeTrue("Одиночка «на одного» занимает комнату целиком");
        plan.GetFreeSpace(seed.Rooms[0], seed.LuxTypeId).ShouldBe(1);

        // Выселение по типу не трогает соседей из сестринского типа.
        await factory.Services.RunAsAsync(seed.OwnerId, sp =>
            sp.GetRequiredService<IAccommodationService>().UnOccupyRoomType(seed.SingleTypeId));
        plan = await LoadPlanAsync(seed.LuxTypeId);
        plan.GetGroup(seed.SingleGroup).RoomId.ShouldBeNull();
        plan.GetGroup(seed.LuxGroup).RoomId.ShouldBe(seed.Rooms[0]);

        // Страницы с общим пулом открываются.
        var (_, ownerEmail) = seed.Owner;
        using var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), ownerEmail);
        (await client.GetAsync($"{seed.ProjectId.Value}/rooms/")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync($"{seed.ProjectId.Value}/rooms/{seed.SingleTypeId.AccommodationTypeId}/details"))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        // Контрол расселения на странице одного типа показывает весь пул с подписями типов.
        var rooms = (await (await client.GetRoomsAsync(seed.SingleTypeId)).Content.ReadFromJsonAsync<RoomTypeRoomsViewModel>())
            .ShouldNotBeNull();
        rooms.SiblingTypeNames.ShouldBe(["Люкс"]);
        rooms.Rooms.SelectMany(room => room.Groups).Concat(rooms.UnassignedGroups)
            .Select(group => (group.GroupId, group.TypeName))
            .ShouldBe([(seed.LuxGroup, "Люкс"), (seed.SingleGroup, "Люкс на одного")], ignoreOrder: true);

        // Удаление одного из типов оставляет категорию и комнаты второму. Выселенная группа
        // удаляемого типа расформировывается (#5359), группа сестринского типа живёт дальше.
        await factory.Services.RunAsAsync(seed.OwnerId, sp =>
            sp.GetRequiredService<IAccommodationTypeService>().DeleteAccommodationType(seed.SingleTypeId));

        plan = await LoadPlanAsync(seed.LuxTypeId);
        plan.AccommodationTypes.ShouldHaveSingleItem().Id.ShouldBe(seed.LuxTypeId);
        plan.Rooms.Count.ShouldBe(2);
        plan.Groups.ShouldHaveSingleItem().Id.ShouldBe(seed.LuxGroup);
        plan.GetGroup(seed.LuxGroup).RoomId.ShouldBe(seed.Rooms[0]);
    }

    private async Task<DomainTypes.Accommodation.RoomCategoryPlan> LoadPlanAsync(AccommodationTypeIdentification typeId)
    {
        using var scope = factory.Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<IRoomCategoryPlanRepository>()
            .GetPlanForTypeOrDefault(typeId)).ShouldNotBeNull();
    }

    private sealed record Seed(
        (UserIdentification UserId, string Email) Owner,
        ProjectIdentification ProjectId,
        AccommodationTypeIdentification LuxTypeId,
        AccommodationTypeIdentification SingleTypeId,
        IReadOnlyList<AccommodationRoomIdentification> Rooms,
        AccommodationRequestIdentification LuxGroup,
        AccommodationRequestIdentification SingleGroup)
    {
        public UserIdentification OwnerId => Owner.UserId;
    }

    private async Task<Seed> SeedAsync()
    {
        var owner = await CreateUserAsync();
        var ownerId = owner.UserId;

        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, ownerId, "Проект с общим пулом комнат");
        }

        await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
            await projectService.SetAccommodationSettings(projectId, enableAccommodation: true);
        });

        var luxTypeId = await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IAccommodationTypeService>().CreateAccommodationType(
                projectId,
                new AccommodationTypeRequest("Люкс", new MarkdownString(""), Cost: 100, Capacity: 2, IsPlayerSelectable: true),
                new NewRoomCategory("Люкс")));

        var (singleTypeId, rooms) = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var metadata = sp.GetRequiredService<IProjectMetadataRepository>();
            var categoryId = (await metadata.GetProjectMetadata(projectId)).AccommodationSettings.GetTypeById(luxTypeId).RoomCategoryId;
            AccommodationTypeIdentification singleTypeId;
            using (var lazyLoads = LazyLoadCounter.BeginScope())
            {
                singleTypeId = await sp.GetRequiredService<IAccommodationTypeService>().CreateAccommodationType(
                    projectId,
                    new AccommodationTypeRequest("Люкс на одного", new MarkdownString(""), Cost: 150, Capacity: 1, IsPlayerSelectable: true),
                    new ExistingRoomCategory(categoryId));
                lazyLoads.Count.ShouldBe(0, "Тип в существующей категории заводится без ленивых загрузок");
            }

            var rooms = await sp.GetRequiredService<IAccommodationService>().AddRooms(categoryId, ["101", "102"]);
            return (singleTypeId, rooms);
        });

        var luxClaim = await CreateClaimAsync(ownerId, projectId);
        var singleClaim = await CreateClaimAsync(ownerId, projectId);

        var groups = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            await claimService.SetAccommodationType(projectId.Value, luxClaim.ClaimId, luxTypeId.AccommodationTypeId);
            await claimService.SetAccommodationType(projectId.Value, singleClaim.ClaimId, singleTypeId.AccommodationTypeId);
            return await AccommodationTestHelpers.GetAccommodationGroupIdsAsync(sp, [luxClaim, singleClaim]);
        });
        var (luxGroup, singleGroup) = (groups[0], groups[1]);

        return new Seed(owner, projectId, luxTypeId, singleTypeId, [.. rooms], luxGroup, singleGroup);
    }

    private async Task<ClaimIdentification> CreateClaimAsync(UserIdentification ownerId, ProjectIdentification projectId)
    {
        var characterId = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        var (playerId, _) = await CreateUserAsync();
        return await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Хочу играть эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: true);
        });
    }

    private async Task<(UserIdentification UserId, string Email)> CreateUserAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
    }
}
