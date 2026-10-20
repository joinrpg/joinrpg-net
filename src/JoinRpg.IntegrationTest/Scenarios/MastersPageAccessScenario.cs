using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.ProjectAccess;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Страниц мастеров две (ADR019, §7): публичная {projectId}/masters — одна для всех, мастерская
/// {projectId}/masters/manage (права, заявки, управление) — только мастерам проекта.
/// </summary>
public class MastersPageAccessScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const string MasterOnlyMarker = "Права доступа";
    private const string ManageLinkText = "Редактировать мастеров";

    [Fact]
    public async Task Anonymous_SeesMastersList_WithoutManagement()
    {
        using var scope = factory.Services.CreateScope();
        var (ownerId, ownerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var html = await GetMastersPage(client, projectId);

        html.ShouldContain("class=\"join-user\"");
        html.ShouldNotContain(MasterOnlyMarker);
        // Контакты — по правилам профиля: аноним email мастера не видит.
        html.ShouldNotContain(ownerEmail);
        // Аватарку компонент не грузит сам — её кладёт UserProfileDetailsBuilder (#5600, #5602).
        html.ShouldContain("class=\"profile-avatar\"");

        // Мастерская страница анониму — на вход.
        await ManagePageShouldRedirectTo(client, projectId, "login");
    }

    [Fact]
    public async Task AuthenticatedNonMaster_SeesMastersList_WithoutManagement()
    {
        using var scope = factory.Services.CreateScope();
        var (ownerId, ownerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);
        var (_, playerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            playerEmail,
            followsRedirects: false);

        var html = await GetMastersPage(client, projectId);

        html.ShouldContain("class=\"join-user\"");
        html.ShouldNotContain(MasterOnlyMarker);
        html.ShouldNotContain(ownerEmail);
        html.ShouldNotContain(ManageLinkText);

        // Мастерская страница немастеру закрыта — отказ, а не падение.
        await ManagePageShouldRedirectTo(client, projectId, "AccessDenied");
    }

    [Fact]
    public async Task Master_PublicPageShowsOnlyPublicMasters_LikeForPlayers()
    {
        // ADR019, §7: на публичной странице мастер видит то же, что игроки; всех мастеров — на мастерской.
        var (projectId, _, ownerEmail, _) = await CreateProjectWithHiddenMaster();

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            ownerEmail,
            followsRedirects: false);
        var html = await GetPage(client, projectId, "masters");

        // Один пользователь в списке — публичный владелец; скрытого мастера нет.
        CountUserLinks(html).ShouldBe(1);
    }

    [Fact]
    public async Task Master_PublicPageIsSameAsForPlayers_WithLinkToManagement()
    {
        using var scope = factory.Services.CreateScope();
        var (ownerId, ownerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            ownerEmail,
            followsRedirects: false);

        var html = await GetMastersPage(client, projectId);

        html.ShouldNotContain(MasterOnlyMarker);
        html.ShouldContain(ManageLinkText);
        html.ShouldContain($"/{projectId.Value}/masters/manage");
    }

    [Fact]
    public async Task Master_SeesManagement_WithLinkToPublicPage()
    {
        using var scope = factory.Services.CreateScope();
        var (ownerId, ownerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            ownerEmail,
            followsRedirects: false);

        var html = WebUtility.HtmlDecode(await GetPage(client, projectId, "masters/manage"));

        html.ShouldContain(MasterOnlyMarker);
        html.ShouldContain("Перейти на публичный список мастеров");
    }

    [Fact]
    public async Task AdminNotMaster_SeesPublicPageWithContacts()
    {
        // Админ сайта, не будучи мастером проекта, видит публичную страницу, а не мастерскую таблицу.
        // Контакты мастеров — как в их профилях: админу видны.
        using var scope = factory.Services.CreateScope();
        var (ownerId, ownerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);
        var (_, adminEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);

        var myDb = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var dbAdmin = myDb.Set<JoinRpg.DataModel.User>().Include("Auth").Single(u => u.Email == adminEmail);
        dbAdmin.Auth.IsAdmin = true;
        await myDb.SaveChangesAsync();

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            adminEmail,
            followsRedirects: false);

        var html = await GetMastersPage(client, projectId);

        html.ShouldNotContain(MasterOnlyMarker);
        html.ShouldContain(ownerEmail);
        html.ShouldNotContain(ManageLinkText);

        // Мастерская страница — только мастерам проекта, админ сайта сам по себе её не открывает.
        await ManagePageShouldRedirectTo(client, projectId, "AccessDenied");
    }

    [Theory]
    [InlineData("masters")]
    [InlineData("Account/AccessDenied?projectId={0}")] // панель «нет доступа»: к кому обратиться за правами
    public async Task Anonymous_DoesNotSeeNonPublicMaster(string page)
    {
        // ADR019, §3: непубличного мастера видят только мастера проекта.
        var (projectId, _, _, _) = await CreateProjectWithHiddenMaster();

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await GetPage(client, projectId, page);

        // Один пользователь в списке — публичный владелец; скрытого мастера нет.
        CountUserLinks(html).ShouldBe(1);
    }

    [Fact]
    public async Task Anonymous_SeesRoleAndSanitizedDescription()
    {
        var (projectId, _, _, ownerId) = await CreateProjectWithHiddenMaster();
        UserIdentification storyMasterId;
        using (var scope = factory.Services.CreateScope())
        {
            storyMasterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = storyMasterId,
                Role = new("Мастер по сюжету"),
                Description = new MarkdownString("Пишите про **сюжет**<script>alert(1)</script>"),
                IsPublic = true,
                Permissions = [Permission.CanManagePlots],
            }));

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var raw = await GetPage(client, projectId, "masters");
        var html = WebUtility.HtmlDecode(raw);

        html.ShouldContain("Мастер по сюжету");
        // Описание отдаётся готовым HTML — проверяем по сырому ответу, иначе декодирование спрятало бы &lt;strong&gt;.
        raw.ShouldContain("<strong>сюжет</strong>");
        // А <script> не должно быть ни в каком виде — по раскодированному это строже.
        html.ShouldNotContain("<script>alert(1)</script>");
    }

    [Fact]
    public async Task MasterTable_ShowsRoleAndHiddenMark()
    {
        var (projectId, _, ownerEmail, _) = await CreateProjectWithHiddenMaster();

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            ownerEmail,
            followsRedirects: false);
        var html = WebUtility.HtmlDecode(await GetPage(client, projectId, "masters/manage"));

        html.ShouldContain("Главный мастер"); // роль создателя игры
        html.ShouldContain("скрыт от игроков");
    }

    /// <summary>Проект, где у владельца (публичного) есть непубличный мастер с правом выдавать доступ.</summary>
    private async Task<(ProjectIdentification ProjectId, UserIdentification HiddenMasterId, string OwnerEmail, UserIdentification OwnerId)> CreateProjectWithHiddenMaster()
    {
        using var scope = factory.Services.CreateScope();
        var (ownerId, ownerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);
        var hiddenMasterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = hiddenMasterId,
                Role = new("Мастер"),
                IsPublic = false,
                Permissions = [Permission.CanManageClaims, Permission.CanGrantRights],
            }));
        return (projectId, hiddenMasterId, ownerEmail, ownerId);
    }

    private static async Task<string> GetPage(HttpClient client, ProjectIdentification projectId, string pageFormat)
    {
        var page = pageFormat.Contains("{0}") ? string.Format(pageFormat, projectId.Value) : $"{projectId.Value}/{pageFormat}";
        var response = await client.GetAsync(page);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task ManagePageShouldRedirectTo(HttpClient client, ProjectIdentification projectId, string target)
    {
        var response = await client.GetAsync($"{projectId.Value}/masters/manage");
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull().ToString().ShouldContain(target, Case.Insensitive);
    }

    private static int CountUserLinks(string html) => html.Split("class=\"join-user\"").Length - 1;

    private static async Task<string> GetMastersPage(HttpClient client, ProjectIdentification projectId)
    {
        var response = await client.GetAsync($"{projectId.Value}/masters");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync();
    }
}
