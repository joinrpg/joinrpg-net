using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Accommodation;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Форма типа проживания — Blazor-остров <see cref="RoomTypeEditForm"/> на MVC-страницах
/// <c>{projectId}/rooms/AddRoomType</c> и <c>{projectId}/rooms/{roomTypeId}/edit</c>: страницы
/// открываются с пререндеренной формой, а создание и изменение идут через ручки
/// <c>webapi/room-type/</c>.
/// </summary>
public class RoomTypeEditScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task CreateAndEditRoomType_ThroughIsland()
    {
        var (ownerId, email) = await CreateUserAsync();
        var projectId = await CreateProjectWithAccommodationAsync(ownerId);
        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), email);

        var addPage = await client.GetAsync($"{projectId.Value}/rooms/AddRoomType");
        addPage.StatusCode.ShouldBe(HttpStatusCode.OK, "Страница добавления типа не открылась");
        (await addPage.Content.ReadAsStringAsync()).ShouldContain("Назад к списку типов поселения");

        var create = await client.PostAsJsonAsync(
            $"webapi/room-type/create?projectId={projectId.Value}",
            new RoomTypeEditViewModel { Name = "Шатёр", Cost = 500, Capacity = 3, Description = "**Тепло**" });
        create.StatusCode.ShouldBe(HttpStatusCode.OK, await create.Content.ReadAsStringAsync());

        var created = (await GetMetadataAsync(projectId)).AccommodationSettings.Types.ShouldHaveSingleItem();
        created.Name.ShouldBe("Шатёр");
        created.Cost.ShouldBe(500);
        created.Capacity.ShouldBe(3);
        created.IsPlayerSelectable.ShouldBeTrue();
        created.Description.Value.ShouldBe("**Тепло**");

        var editPage = await client.GetAsync($"{projectId.Value}/rooms/{created.Id.AccommodationTypeId}/edit");
        editPage.StatusCode.ShouldBe(HttpStatusCode.OK, "Страница изменения типа не открылась");
        var editHtml = await editPage.AsHtmlDocument();
        // Пререндер острова уже подставил текущее название в поле формы. Кириллица в атрибутах
        // приходит числовыми сущностями, поэтому значения сначала раскодируем.
        (editHtml.DocumentNode.SelectNodes("//input[@type='text']") ?? Enumerable.Empty<HtmlAgilityPack.HtmlNode>())
            .Select(input => WebUtility.HtmlDecode(input.GetAttributeValue("value", "")))
            .ShouldContain("Шатёр", "Форма изменения не показала текущее название типа");

        var update = await client.PostAsJsonAsync(
            $"webapi/room-type/update?projectId={projectId.Value}&roomTypeId={created.Id.AccommodationTypeId}",
            new RoomTypeEditViewModel { Name = "Большой шатёр", Cost = 700, Capacity = 5, IsPlayerSelectable = false, Description = "" });
        update.StatusCode.ShouldBe(HttpStatusCode.OK, await update.Content.ReadAsStringAsync());

        var updated = (await GetMetadataAsync(projectId)).AccommodationSettings.GetTypeById(created.Id);
        updated.Name.ShouldBe("Большой шатёр");
        updated.Cost.ShouldBe(700);
        updated.Capacity.ShouldBe(5);
        updated.IsPlayerSelectable.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateRoomType_InvalidForm_ReturnsRussianMessage()
    {
        var (ownerId, email) = await CreateUserAsync();
        var projectId = await CreateProjectWithAccommodationAsync(ownerId);
        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), email);

        var response = await client.PostAsJsonAsync(
            $"webapi/room-type/create?projectId={projectId.Value}",
            new RoomTypeEditViewModel { Name = "Шатёр", Capacity = 0 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync())
            .ShouldContain("Укажите количество мест в номере — целое число от 1 до 1000");
        (await GetMetadataAsync(projectId)).AccommodationSettings.Types.ShouldBeEmpty();
    }

    /// <summary>
    /// Мастеру без права <c>CanManageAccommodation</c> мутации недоступны, даже если все
    /// остальные права у него есть. Отказ даёт атрибут на экшене, до сервиса дело не доходит.
    /// </summary>
    [Fact]
    public async Task MutationsWithoutPermission_AreForbidden()
    {
        var (ownerId, _) = await CreateUserAsync();
        var projectId = await CreateProjectWithAccommodationAsync(ownerId);
        var typeId = await CreateTypeAsync(ownerId, projectId, "Палатка");

        var (restrictedMasterId, restrictedEmail) = await CreateUserAsync();
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = restrictedMasterId,
                Role = new("Мастер"),
                Permissions = [.. Enum.GetValues<Permission>()
                    .Where(p => p is not Permission.None and not Permission.CanManageAccommodation)],
            }));
        var client = await CreateClientAsync(restrictedEmail);

        var create = await client.PostAsJsonAsync(
            $"webapi/room-type/create?projectId={projectId.Value}",
            new RoomTypeEditViewModel { Name = "Чужой шатёр", Capacity = 2 });
        ShouldBeDenied(create, "Создание без права должно быть запрещено");

        var update = await client.PostAsJsonAsync(
            $"webapi/room-type/update?projectId={projectId.Value}&roomTypeId={typeId.AccommodationTypeId}",
            new RoomTypeEditViewModel { Name = "Переименовано", Capacity = 2 });
        ShouldBeDenied(update, "Изменение без права должно быть запрещено");

        var types = (await GetMetadataAsync(projectId)).AccommodationSettings.Types;
        types.ShouldHaveSingleItem().Name.ShouldBe("Палатка", "Без права ничего не должно было измениться");
    }

    /// <summary>
    /// Чтение формы — только мастерам проекта: посторонний пользователь получает отказ.
    /// </summary>
    [Fact]
    public async Task GetByOutsider_IsForbidden()
    {
        var (ownerId, _) = await CreateUserAsync();
        var projectId = await CreateProjectWithAccommodationAsync(ownerId);
        var typeId = await CreateTypeAsync(ownerId, projectId, "Палатка");

        var (_, outsiderEmail) = await CreateUserAsync();
        var client = await CreateClientAsync(outsiderEmail);

        var response = await client.GetAsync(
            $"webapi/room-type/get?projectId={projectId.Value}&roomTypeId={typeId.AccommodationTypeId}");

        ShouldBeDenied(response, "Постороннему чтение формы должно быть запрещено");
    }

    /// <summary>
    /// Номер типа из чужого проекта, подставленный к своему проекту, указывает в пустоту: права
    /// проверены по своему проекту, но и тип ищется только в нём.
    /// </summary>
    [Fact]
    public async Task UpdateWithAlienRoomType_DoesNotTouchIt()
    {
        var (ownerId, email) = await CreateUserAsync();
        var projectId = await CreateProjectWithAccommodationAsync(ownerId);

        var (alienOwnerId, _) = await CreateUserAsync();
        var alienProjectId = await CreateProjectWithAccommodationAsync(alienOwnerId);
        var alienTypeId = await CreateTypeAsync(alienOwnerId, alienProjectId, "Чужая палатка");

        var client = await CreateClientAsync(email);

        var response = await client.PostAsJsonAsync(
            $"webapi/room-type/update?projectId={projectId.Value}&roomTypeId={alienTypeId.AccommodationTypeId}",
            new RoomTypeEditViewModel { Name = "Подменено", Capacity = 2 });

        response.StatusCode.ShouldNotBe(HttpStatusCode.OK, "Чужой тип не должен находиться через свой проект");
        (await GetMetadataAsync(alienProjectId)).AccommodationSettings.GetTypeById(alienTypeId)
            .Name.ShouldBe("Чужая палатка", "Чужой тип не должен был измениться");
        (await GetMetadataAsync(projectId)).AccommodationSettings.Types.ShouldBeEmpty();
    }

    /// <summary>
    /// Отказ по правам: <c>webapi/</c> отвечает 403, а не редиректом на «нет доступа» (#5392).
    /// </summary>
    private static void ShouldBeDenied(HttpResponseMessage response, string message)
        => response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, message);

    /// <summary>
    /// Клиент, который не ходит по редиректам: иначе отказ превратился бы в 200 страницы ошибки.
    /// </summary>
    private Task<HttpClient> CreateClientAsync(string email)
        => TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            email,
            followsRedirects: false);

    private Task<AccommodationTypeIdentification> CreateTypeAsync(
        UserIdentification ownerId,
        ProjectIdentification projectId,
        string name)
        => factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IAccommodationTypeService>().CreateAccommodationType(
                projectId,
                new AccommodationTypeRequest(name, new MarkdownString(""), Cost: 0, Capacity: 2, IsPlayerSelectable: true)));

    private async Task<ProjectInfo> GetMetadataAsync(ProjectIdentification projectId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
    }

    private async Task<(UserIdentification UserId, string Email)> CreateUserAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
    }

    private async Task<ProjectIdentification> CreateProjectWithAccommodationAsync(UserIdentification ownerId)
    {
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, ownerId, "Проект для формы типа проживания");
        }

        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectService>().SetAccommodationSettings(projectId, enableAccommodation: true));

        return projectId;
    }
}
