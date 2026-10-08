using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;
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
/// адресам должен отдавать 405, иначе переделка тихо откатится;
/// </description></item>
/// <item><description>
/// antiforgery: глобальный <c>AutoValidateAntiforgeryTokenAttribute</c> закрывает и ajax-ручки, у
/// которых нет своего атрибута. Без токена запрос не должен ничего менять;
/// </description></item>
/// <item><description>
/// привязка модели: <c>ProjectEntityIdModelBinder</c> склеивает голое число из query с проектом
/// маршрута. Это и есть защита от дефекта 2 ADR018 — комнату чужого проекта подсунуть нельзя,
/// причём проверить это можно только сквозным запросом;
/// </description></item>
/// <item><description>
/// раскладка доменных исключений по кодам ответа: переполнение — 400, ненайденное — 404;
/// </description></item>
/// <item><description>
/// результат виден на странице: у строки комнаты меняется атрибут <c>occupancy</c>, по которому
/// живёт <c>rooms.js</c>.
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
        var occupy = await client.PostWithTokenHeaderAsync(
            seed.OccupyUrl(seed.RoomIds[0], seed.RequestIds[0], seed.RequestIds[1]),
            token);
        occupy.StatusCode.ShouldBe(HttpStatusCode.OK, "Заселение не прошло");
        (await seed.GetOccupancyAsync(client, seed.RoomIds[0])).ShouldBe(2);

        // Выселение одной группы: в комнате должен остаться второй жилец.
        var unoccupyGroup = await client.PostWithTokenHeaderAsync(
            seed.UnOccupyGroupUrl(seed.RequestIds[0]),
            token);
        unoccupyGroup.StatusCode.ShouldBe(HttpStatusCode.OK, "Выселение группы не прошло");
        (await seed.GetOccupancyAsync(client, seed.RoomIds[0])).ShouldBe(1);

        // Выселение по типу проживания — обычная форма, поэтому успех это 302 на страницу комнат.
        var unoccupyType = await client.PostFormAsync(
            $"{seed.ProjectId.Value}/rooms/UnOccupyRoomsByType",
            token,
            ("roomTypeId", seed.RoomTypeId.ToString()));
        unoccupyType.StatusCode.ShouldBe(
            HttpStatusCode.Found,
            $"Выселение по типу не прошло: {await unoccupyType.DescribeValidationErrorsAsync()}");
        (await seed.GetOccupancyAsync(client, seed.RoomIds[0])).ShouldBe(0);

        // Выселение по проекту: чтобы шаг что-то делал, сначала снова заселяем.
        var occupyAgain = await client.PostWithTokenHeaderAsync(
            seed.OccupyUrl(seed.RoomIds[1], seed.RequestIds[2]),
            token);
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
    /// Отказы заселения: переполнение, пустой список групп и комната чужого проекта. Проверяется
    /// именно раскладка по кодам ответа — её делает контроллер, а не сервис.
    /// </summary>
    [Fact]
    public async Task OccupyRoom_Refusals()
    {
        var seed = await SeedAsync();
        var client = await seed.CreateClientAsync(factory);
        var token = await client.GetAntiforgeryTokenAsync(AntiforgeryTokenPage);

        // Три заявки в комнату на двоих: не влезает, сервис бросает JoinRpgInsufficientRoomSpaceException.
        var overfull = await client.PostWithTokenHeaderAsync(
            seed.OccupyUrl(seed.RoomIds[0], [.. seed.RequestIds]),
            token);
        overfull.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "Переполненная комната должна давать 400");
        (await seed.GetOccupancyAsync(client, seed.RoomIds[0])).ShouldBe(
            0,
            "Операция не влезла целиком — в комнату не должен въехать никто");

        // Пустой список групп до сервиса не доходит: это невалидный запрос.
        var noGroups = await client.PostWithTokenHeaderAsync(
            $"{seed.ProjectId.Value}/rooms/occupyroom?roomTypeId={seed.RoomTypeId}&room={seed.RoomIds[0]}&reqId=",
            token);
        noGroups.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "Заселение без групп должно давать 400");

        // Комната из другого проекта: в query уезжает голое число, а проект берётся из маршрута,
        // поэтому склеенный идентификатор указывает в пустоту (дефект 2 ADR018).
        var alien = await SeedAsync();
        var alienRoom = await client.PostWithTokenHeaderAsync(
            seed.OccupyUrl(alien.RoomIds[0], seed.RequestIds[0]),
            token);
        alienRoom.StatusCode.ShouldBe(
            HttpStatusCode.NotFound,
            "Комната чужого проекта не должна находиться по номеру");

        // Заявка на проживание из чужого проекта — тоже 404, и по той же причине.
        var alienRequest = await client.PostWithTokenHeaderAsync(
            seed.OccupyUrl(seed.RoomIds[0], alien.RequestIds[0]),
            token);
        alienRequest.StatusCode.ShouldBe(
            HttpStatusCode.NotFound,
            "Заявка на проживание чужого проекта не должна находиться по номеру");
    }

    /// <summary>
    /// Без antiforgery-токена мутирующие ручки поселения не должны ничего менять.
    /// </summary>
    /// <remarks>
    /// У <c>OccupyRoom</c> и <c>UnOccupyGroup</c> нет своего <c>[ValidateAntiForgeryToken]</c> —
    /// их закрывает глобальный фильтр из <c>Startup</c>. Если фильтр однажды снимут, тест упадёт.
    /// Отказ выглядит как 302 на <c>/error/antiforgery</c>: результат подменяет
    /// <c>RedirectAntiforgeryValidationFailedResultFilter</c>, чтобы человек увидел объяснение, а
    /// не пустой 400.
    /// </remarks>
    [Fact]
    public async Task OccupyRoom_WithoutAntiforgeryToken_DoesNothing()
    {
        var seed = await SeedAsync();
        var client = await seed.CreateClientAsync(factory);

        var response = await client.PostWithTokenHeaderAsync(
            seed.OccupyUrl(seed.RoomIds[0], seed.RequestIds[0]),
            antiforgeryToken: null);

        response.StatusCode.ShouldBe(
            HttpStatusCode.Found,
            "Заселение без antiforgery-токена не должно приниматься");
        response.Headers.Location?.ToString().ShouldBe("/error/antiforgery");
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
            seed.OccupyUrl(seed.RoomIds[0], seed.RequestIds[0]),
            seed.UnOccupyGroupUrl(seed.RequestIds[0]),
            $"{seed.ProjectId.Value}/rooms/UnOccupyRoomsByType?roomTypeId={seed.RoomTypeId}",
            $"{seed.ProjectId.Value}/rooms/UnOccupyAll?projectId={seed.ProjectId.Value}",
        })
        {
            var response = await client.GetAsync(url);

            // Именно 404, а не 405: GET-эндпоинта по этому адресу в портале нет вообще, и до
            // экшена запрос не доходит. Важно, что не 200 и не редирект «сделано».
            response.StatusCode.ShouldBe(
                HttpStatusCode.NotFound,
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
    /// Право проверяется дважды — атрибутом <c>MasterAuthorize</c> на экшене и самим сервисом.
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

        var response = await client.PostWithTokenHeaderAsync(
            seed.OccupyUrl(seed.RoomIds[0], seed.RequestIds[0]),
            token);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect, "Мастеру без права должен уйти отказ");
        response.Headers.Location?.ToString().ShouldContain(
            "AccessDenied",
            customMessage: "Отказ должен вести на страницу «нет доступа», а не на вход");

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

        /// <summary>Адрес заселения — ровно такой, какой собирает <c>rooms.js</c>.</summary>
        public string OccupyUrl(int roomId, params int[] requestIds)
            => $"{ProjectId.Value}/rooms/occupyroom?roomTypeId={RoomTypeId}&room={roomId}"
                + $"&reqId={string.Join(',', requestIds)}";

        public string UnOccupyGroupUrl(int requestId)
            => $"{ProjectId.Value}/rooms/unoccupyroom?roomTypeId={RoomTypeId}&reqId={requestId}";

        public Task<HttpClient> CreateClientAsync(JoinApplicationFactory factory)
            => TestUserProjectHelpers.CreateAuthenticatedClientAsync(
                factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
                OwnerEmail,
                followsRedirects: false);

        /// <summary>
        /// Сколько человек живёт в комнате по данным страницы: атрибут <c>occupancy</c> у строки
        /// комнаты. Именно его читает <c>rooms.js</c>, поэтому проверять состояние по странице
        /// честнее, чем запросом в базу.
        /// </summary>
        public async Task<int> GetOccupancyAsync(HttpClient client, int roomId)
        {
            var response = await client.GetAsync(RoomTypeDetailsUrl);
            response.StatusCode.ShouldBe(HttpStatusCode.OK, $"Страница {RoomTypeDetailsUrl} не открылась");

            var document = await response.AsHtmlDocument();

            // HtmlAgilityPack приводит имена атрибутов к нижнему регистру.
            var row = document.DocumentNode.SelectSingleNode($"//tr[@roomid='{roomId}']")
                ?? throw new InvalidOperationException(
                    $"На странице {RoomTypeDetailsUrl} нет комнаты {roomId}");

            return row.GetAttributeValue("occupancy", -1);
        }
    }
}
