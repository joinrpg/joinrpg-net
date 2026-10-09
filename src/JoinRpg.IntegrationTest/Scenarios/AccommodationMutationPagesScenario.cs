using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Accommodation.Rooms;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Мутирующие ручки поселения под мастером: заселение, выселение группы, выселение по типу и по
/// проекту — вместе с отказами.
/// </summary>
/// <remarks>
/// Сам сервис расселения покрыт юнит-тестами подробно
/// (<c>src/JoinRpg.Services.Impl.Test/Accommodation/</c>), и дублировать их тут нечего. Здесь
/// проверяется ровно то, чего юнит-тест не видит и что целиком переделал ADR018:
/// <list type="bullet">
/// <item><description>
/// маршрутизация: заселение и выселение стали POST-only, раньше это были GET-ссылки. GET по тем же
/// адресам обслуживаться не должен, иначе переделка тихо откатится;
/// </description></item>
/// <item><description>
/// antiforgery: глобальный <c>AutoValidateAntiforgeryTokenAttribute</c> закрывает и ручки острова,
/// у которых нет своего атрибута. Без токена запрос не должен ничего менять;
/// </description></item>
/// <item><description>
/// проект запроса: идентификаторы из тела сверяются с проектом из адреса. Это и есть защита от
/// дефекта 2 ADR018 — комнату чужого проекта подсунуть нельзя, причём проверить это можно только
/// сквозным запросом;
/// </description></item>
/// <item><description>
/// раскладка доменных исключений по кодам ответа: переполнение — 400, ненайденное — 404;
/// </description></item>
/// <item><description>
/// результат виден на странице: у строки комнаты меняется счётчик «занято / вместимость» в
/// пререндеренной разметке острова.
/// </description></item>
/// </list>
/// Сид тут свой, не смоучный: смоук один на все свои сценарии и только читает, а мутации ломали бы
/// его проверки (<see cref="AccommodationPagesLazyLoadsScenario"/>) и зависели бы от порядка тестов.
/// </remarks>
public class AccommodationMutationPagesScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    private const int RoomCapacity = 2;

    /// <summary>
    /// Страница, с которой берётся antiforgery-токен: токен привязан к cookie клиента, а не к
    /// странице, поэтому подходит любая форма.
    /// </summary>
    /// <remarks>
    /// Нарочно не страница комнат: на ней ещё живёт N+1 по нерасселённым заявкам (#4670), его размер
    /// зависит от размера сида, и сид этого сценария упирался бы в снапшот
    /// <c>lazy-loads-baseline.json</c>, снятый на смоучном. Форма создания проекта доступна любому
    /// вошедшему пользователю и не зависит от прав в проекте — это же нужно проверке отказа по правам.
    /// </remarks>
    private const string AntiforgeryTokenPage = "game/create";

    /// <summary>
    /// Полный круг: заселили двоих, выселили одного, выселили остаток по типу, выселили всё по
    /// проекту. После каждого шага смотрим на страницу комнат, а не только на код ответа.
    /// </summary>
    [Fact]
    public async Task OccupyAndEvict_RoundTrip()
    {
        var seed = await SeedAsync();
        var client = await seed.CreateClientAsync(factory);
        var token = await client.GetAntiforgeryTokenAsync(AntiforgeryTokenPage);

        // Заселение двух заявок одной операцией: комната ровно по вместимости.
        var occupy = await client.OccupyAsync(token, seed.ProjectId, seed.Room(0), seed.Request(0), seed.Request(1));
        occupy.StatusCode.ShouldBe(HttpStatusCode.OK, "Заселение не прошло");
        (await seed.GetOccupancyAsync(client, seed.RoomIds[0])).ShouldBe(2);

        // Выселение одной группы: в комнате должен остаться второй жилец.
        var unoccupyGroup = await client.UnOccupyGroupAsync(token, seed.Request(0));
        unoccupyGroup.StatusCode.ShouldBe(HttpStatusCode.OK, "Выселение группы не прошло");
        (await seed.GetOccupancyAsync(client, seed.RoomIds[0])).ShouldBe(1);

        // Выселение по типу проживания: раньше это была форма страницы комнат, теперь ручка острова.
        var unoccupyType = await client.UnOccupyRoomTypeAsync(token, seed.TypeId);
        unoccupyType.StatusCode.ShouldBe(HttpStatusCode.OK, "Выселение по типу не прошло");
        (await seed.GetOccupancyAsync(client, seed.RoomIds[0])).ShouldBe(0);

        // Выселение по проекту: чтобы шаг что-то делал, сначала снова заселяем.
        var occupyAgain = await client.OccupyAsync(token, seed.ProjectId, seed.Room(1), seed.Request(2));
        occupyAgain.StatusCode.ShouldBe(HttpStatusCode.OK, "Повторное заселение не прошло");
        (await seed.GetOccupancyAsync(client, seed.RoomIds[1])).ShouldBe(1);

        var unoccupyAll = await client.PostFormAsync(
            $"{seed.ProjectId.Value}/rooms/UnOccupyAll",
            token,
            ("projectId", seed.ProjectId.Value.ToString()));
        unoccupyAll.StatusCode.ShouldBe(
            HttpStatusCode.Found,
            $"Выселение по проекту не прошло: {await unoccupyAll.DescribeValidationErrorsAsync()}");
        (await seed.GetOccupancyAsync(client, seed.RoomIds[1])).ShouldBe(0);
    }

    /// <summary>
    /// Управление комнатами и чтение модели острова: привязка типа из query, добавление по
    /// списку, переименование, удаление по голому идентификатору в теле, освобождение комнаты.
    /// </summary>
    [Fact]
    public async Task RoomManagement_RoundTrip()
    {
        var seed = await SeedAsync();
        var client = await seed.CreateClientAsync(factory);
        var token = await client.GetAntiforgeryTokenAsync(AntiforgeryTokenPage);

        // Модель острова: тип проживания приходит в query голым числом, проект берётся из адреса.
        var initial = await ReadRoomsAsync(await client.GetRoomsAsync(seed.TypeId));
        initial.Rooms.Select(r => r.RoomId.RoomId).ShouldBe(seed.RoomIds, ignoreOrder: true);
        initial.UnassignedGroups.Count.ShouldBe(3);

        // Добавление: строку «5-6» разбирает сервер, в ответ приходит новое состояние целиком.
        var added = await ReadRoomsAsync(await client.AddRoomsAsync(token, seed.TypeId, "5-6"));
        added.Rooms.Select(r => r.Name).ShouldBe(["1", "2", "5", "6"], ignoreOrder: true);
        var newRoom = added.Rooms.Single(r => r.Name == "5").RoomId;

        (await client.AddRoomsAsync(token, seed.TypeId, " , ")).StatusCode.ShouldBe(
            HttpStatusCode.BadRequest,
            "Пустой список комнат должен давать 400");

        // Переименование видно на странице комнат.
        var rename = await client.RenameRoomAsync(token, newRoom, "Люкс");
        rename.StatusCode.ShouldBe(HttpStatusCode.OK, "Переименование не прошло");
        (await ReadRoomsAsync(await client.GetRoomsAsync(seed.TypeId))).Rooms
            .Single(r => r.RoomId == newRoom).Name.ShouldBe("Люкс");

        // Удаление: в теле — сам идентификатор комнаты, без обёртки.
        var delete = await client.DeleteRoomAsync(token, newRoom);
        delete.StatusCode.ShouldBe(HttpStatusCode.OK, "Удаление пустой комнаты не прошло");
        (await ReadRoomsAsync(await client.GetRoomsAsync(seed.TypeId))).Rooms
            .ShouldNotContain(r => r.RoomId == newRoom);
        (await client.DeleteRoomAsync(token, newRoom)).StatusCode.ShouldBe(
            HttpStatusCode.NotFound,
            "Удалённой комнаты больше нет");

        // Заселённую комнату удалить нельзя, а освободить — можно.
        (await client.OccupyAsync(token, seed.ProjectId, seed.Room(0), seed.Request(0))).StatusCode
            .ShouldBe(HttpStatusCode.OK, "Заселение не прошло");
        (await client.DeleteRoomAsync(token, seed.Room(0))).StatusCode.ShouldBe(
            HttpStatusCode.BadRequest,
            "Заселённую комнату удалять нельзя");

        var evict = await client.UnOccupyRoomAsync(token, seed.Room(0));
        evict.StatusCode.ShouldBe(HttpStatusCode.OK, "Освобождение комнаты не прошло");
        (await seed.GetOccupancyAsync(client, seed.RoomIds[0])).ShouldBe(0);
    }

    private static async Task<RoomTypeRoomsViewModel> ReadRoomsAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"Ручка {response.RequestMessage?.RequestUri} не ответила");
        return await response.Content.ReadFromJsonAsync<RoomTypeRoomsViewModel>()
            ?? throw new InvalidOperationException("Пустой ответ");
    }

    /// <summary>
    /// Отказы заселения: переполнение, пустой список групп, комната и заявка чужого проекта.
    /// Проверяется именно раскладка по кодам ответа — её делает контроллер, а не сервис.
    /// </summary>
    [Fact]
    public async Task OccupyRoom_Refusals()
    {
        var seed = await SeedAsync();
        var client = await seed.CreateClientAsync(factory);
        var token = await client.GetAntiforgeryTokenAsync(AntiforgeryTokenPage);

        // Три заявки в комнату на двоих: не влезает, сервис бросает JoinRpgInsufficientRoomSpaceException.
        var overfull = await client.OccupyAsync(
            token, seed.ProjectId, seed.Room(0), seed.Request(0), seed.Request(1), seed.Request(2));
        overfull.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "Переполненная комната должна давать 400");
        (await seed.GetOccupancyAsync(client, seed.RoomIds[0])).ShouldBe(
            0,
            "Операция не влезла целиком — в комнату не должен въехать никто");

        // Пустой список групп до сервиса не доходит: это невалидный запрос.
        var noGroups = await client.OccupyAsync(token, seed.ProjectId, seed.Room(0));
        noGroups.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "Заселение без групп должно давать 400");

        // Комната из другого проекта (дефект 2 ADR018). Полный идентификатор чужого проекта
        // контроллер не принимает: проект в теле не совпал с проектом в адресе.
        var alien = await SeedAsync();
        var alienRoom = await client.OccupyAsync(token, seed.ProjectId, alien.Room(0), seed.Request(0));
        alienRoom.StatusCode.ShouldBe(
            HttpStatusCode.BadRequest,
            "Комнату чужого проекта нельзя подсунуть в запрос своего");

        // А номер чужой комнаты, приклеенный к своему проекту, указывает в пустоту.
        var splicedRoom = await client.OccupyAsync(
            token, seed.ProjectId, new AccommodationRoomIdentification(seed.ProjectId, alien.RoomIds[0]), seed.Request(0));
        splicedRoom.StatusCode.ShouldBe(
            HttpStatusCode.NotFound,
            "Комната чужого проекта не должна находиться по номеру");

        // Заявка на проживание из чужого проекта — по тем же двум причинам.
        var alienRequest = await client.OccupyAsync(token, seed.ProjectId, seed.Room(0), alien.Request(0));
        alienRequest.StatusCode.ShouldBe(
            HttpStatusCode.BadRequest,
            "Заявку на проживание чужого проекта нельзя подсунуть в запрос своего");

        var splicedRequest = await client.OccupyAsync(
            token, seed.ProjectId, seed.Room(0), new AccommodationRequestIdentification(seed.ProjectId, alien.RequestIds[0]));
        splicedRequest.StatusCode.ShouldBe(
            HttpStatusCode.NotFound,
            "Заявка на проживание чужого проекта не должна находиться по номеру");

        (await seed.GetOccupancyAsync(client, seed.RoomIds[0])).ShouldBe(0, "Отказы ничего не должны менять");
    }

    /// <summary>
    /// Без antiforgery-токена мутирующие ручки поселения не должны ничего менять.
    /// </summary>
    /// <remarks>
    /// У ручек острова нет своего <c>[ValidateAntiForgeryToken]</c> —
    /// их закрывает глобальный фильтр из <c>Startup</c>. Если фильтр однажды снимут, тест упадёт.
    /// Для <c>/webapi</c> отказ — 400 с причиной текстом (её показывает остров), а не редирект на
    /// страницу ошибки: редирект клиент острова прошёл бы до 200 и счёл бы операцию успешной (#5407).
    /// </remarks>
    [Fact]
    public async Task OccupyRoom_WithoutAntiforgeryToken_DoesNothing()
    {
        var seed = await SeedAsync();
        var client = await seed.CreateClientAsync(factory);

        var response = await client.OccupyAsync(
            antiforgeryToken: null, seed.ProjectId, seed.Room(0), seed.Request(0));

        response.StatusCode.ShouldBe(
            HttpStatusCode.BadRequest,
            "Заселение без antiforgery-токена не должно приниматься");
        (await response.Content.ReadAsStringAsync()).ShouldContain("Сессия устарела");
        (await seed.GetOccupancyAsync(client, seed.RoomIds[0])).ShouldBe(0);
    }

    /// <summary>
    /// Заселение и выселение доступны только POST: до ADR018 это были GET-ссылки, и возврат к ним
    /// означал бы, что состояние снова меняется переходом по ссылке.
    /// </summary>
    [Fact]
    public async Task OccupyAndEvictRoutes_RejectGet()
    {
        var seed = await SeedAsync();
        var client = await seed.CreateClientAsync(factory);

        foreach (var url in new[]
        {
            AccommodationRoomsApi.Url(seed.ProjectId, "OccupyRoom"),
            AccommodationRoomsApi.Url(seed.ProjectId, "UnOccupyGroup"),
            AccommodationRoomsApi.Url(seed.ProjectId, "UnOccupyRoom"),
            AccommodationRoomsApi.Url(seed.ProjectId, "UnOccupyRoomType"),
            $"{seed.ProjectId.Value}/rooms/UnOccupyAll?projectId={seed.ProjectId.Value}",
        })
        {
            var response = await client.GetAsync(url);

            // GET-эндпоинта по этому адресу нет, и до экшена запрос не доходит. Важно, что
            // не 200 и не редирект «сделано».
            response.StatusCode.ShouldBeOneOf(
                [HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed],
                $"GET {url} не должен обслуживаться — операция меняет состояние");
        }

        (await seed.GetOccupancyAsync(client, seed.RoomIds[0])).ShouldBe(
            0,
            "GET по адресам заселения ничего не должен менять");
    }

    /// <summary>
    /// Мастеру без права <c>CanSetPlayersAccommodations</c> ручки поселения недоступны, даже если
    /// все остальные права у него есть.
    /// </summary>
    /// <remarks>
    /// Право проверяется дважды — атрибутом <c>RequireMaster</c> на экшене и самим сервисом.
    /// Юнит-тест видит только вторую проверку; тут важно, что до сервиса дело не доходит и мастер
    /// получает редирект на страницу «нет доступа», а не 500.
    /// </remarks>
    [Fact]
    public async Task OccupyRoom_WithoutPermission_IsDenied()
    {
        var seed = await SeedAsync();

        var (restrictedMasterId, restrictedEmail) = await CreateUserAsync();
        await factory.Services.RunAsAsync(seed.OwnerId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = seed.ProjectId,
                UserId = restrictedMasterId,
                Role = new("Мастер"),
                Permissions = [.. Enum.GetValues<Permission>()
                    .Where(p => p is not Permission.None and not Permission.CanSetPlayersAccommodations)],
            }));

        var client = await CreateClientAsync(restrictedEmail);

        // Токен берём со страницы создания проекта: она доступна любому вошедшему пользователю и
        // не зависит от прав в проекте (на странице комнат кнопки выселения ему уже не покажут).
        var token = await client.GetAntiforgeryTokenAsync(AntiforgeryTokenPage);

        var response = await client.OccupyAsync(token, seed.ProjectId, seed.Room(0), seed.Request(0));

        // webapi отвечает на отказ 403, а не редиректом на «нет доступа» (#5392).
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "Мастеру без права должен уйти отказ");

        // Ничего не изменилось — проверяем клиентом владельца, у которого страница открывается.
        var ownerClient = await seed.CreateClientAsync(factory);
        (await seed.GetOccupancyAsync(ownerClient, seed.RoomIds[0])).ShouldBe(0);
    }

    /// <summary>
    /// Поднимает отдельный проект с поселением: тип проживания на <see cref="RoomCapacity"/> мест,
    /// две комнаты и три заявки на проживание.
    /// </summary>
    /// <remarks>
    /// Три заявки на комнату из двух мест — чтобы переполнение проверялось на честных данных, а не
    /// на подделанном идентификаторе.
    /// </remarks>
    private async Task<AccommodationSeed> SeedAsync()
    {
        var (ownerId, ownerEmail) = await CreateUserAsync();

        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, ownerId, "Проект для мутаций поселения");
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

        var roomTypeId = await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IAccommodationTypeService>().CreateAccommodationType(
                projectId,
                new AccommodationTypeRequest(
                    "Двушка",
                    new MarkdownString("Комната на двоих"),
                    Cost: 100,
                    Capacity: RoomCapacity,
                    IsPlayerSelectable: true)));

        // Комнаты создаются в категории, а не в типе проживания: категорию по типу знают только
        // метаданные (ADR018, §2). Сервис возвращает идентификаторы — в базу лазить не нужно.
        var roomIds = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                .GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IAccommodationService>().AddRooms(
                projectInfo.AccommodationSettings.GetTypeById(roomTypeId).RoomCategoryId,
                ["1", "2"]);
        });

        var characters = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                .GetProjectMetadata(projectId);
            var characterService = sp.GetRequiredService<ICharacterService>();
            var result = new List<CharacterIdentification>(3);
            for (var i = 1; i <= 3; i++)
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

        var claims = new List<ClaimIdentification>(characters.Count);
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

        var requestIds = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            foreach (var claimId in claims)
            {
                await claimService.SetAccommodationType(
                    projectId.Value, claimId.ClaimId, roomTypeId.AccommodationTypeId);
            }

            var groupIds = await AccommodationTestHelpers.GetAccommodationGroupIdsAsync(sp, claims);
            return groupIds.Select(groupId => groupId.AccommodationRequestId).ToList();
        });

        return new AccommodationSeed(
            ownerId,
            ownerEmail,
            projectId,
            roomTypeId.AccommodationTypeId,
            [.. roomIds.Select(r => r.RoomId)],
            requestIds);
    }

    private async Task<(UserIdentification UserId, string Email)> CreateUserAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
    }

    /// <summary>
    /// Клиент под указанным пользователем. Редиректы намеренно не проходит: успех формы и отказ по
    /// правам — это оба 302, и переход по ним подменил бы проверяемый ответ.
    /// </summary>
    private Task<HttpClient> CreateClientAsync(string email)
        => TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            email,
            followsRedirects: false);

    /// <summary>Проект с поселением, поднятый под один тест.</summary>
    private sealed record AccommodationSeed(
        UserIdentification OwnerId,
        string OwnerEmail,
        ProjectIdentification ProjectId,
        int RoomTypeId,
        IReadOnlyList<int> RoomIds,
        IReadOnlyList<int> RequestIds)
    {
        /// <summary>Страница комнат типа проживания.</summary>
        public string RoomTypeDetailsUrl => $"{ProjectId.Value}/rooms/{RoomTypeId}/details";

        public AccommodationTypeIdentification TypeId => new(ProjectId, RoomTypeId);

        public AccommodationRoomIdentification Room(int index) => new(ProjectId, RoomIds[index]);

        public AccommodationRequestIdentification Request(int index) => new(ProjectId, RequestIds[index]);

        public Task<HttpClient> CreateClientAsync(JoinApplicationFactory factory)
            => TestUserProjectHelpers.CreateAuthenticatedClientAsync(
                factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
                OwnerEmail,
                followsRedirects: false);

        /// <summary>Сколько человек живёт в комнате по данным страницы комнат</summary>
        public Task<int> GetOccupancyAsync(HttpClient client, int roomId)
            => client.GetOccupancyAsync(RoomTypeDetailsUrl, roomId);
    }
}
