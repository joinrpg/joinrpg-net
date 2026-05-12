using System.Net;
using HtmlAgilityPack;
using Joinrpg.Web.Identity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace JoinRpg.IntegrationTests.TestInfrastructure;

public static class TestUserProjectHelpers
{
    /// <summary>
    /// Создаёт тестового пользователя и возвращает его идентификатор.
    /// </summary>
    /// <param name="serviceProvider">Service provider для получения зависимостей.</param>
    /// <param name="email">Email пользователя (если null, генерируется автоматически).</param>
    /// <param name="password">Пароль пользователя.</param>
    /// <returns>Идентификатор созданного пользователя.</returns>
    public static async Task<UserIdentification> CreateTestUserAsync(
        IServiceProvider serviceProvider,
        string? email = null,
        string password = "Password123!")
    {
        var (userId, _) = await CreateTestUserWithEmailAsync(serviceProvider, email, password);
        return userId;
    }

    /// <summary>
    /// Создаёт тестового пользователя и возвращает его идентификатор и email.
    /// </summary>
    /// <param name="serviceProvider">Service provider для получения зависимостей.</param>
    /// <param name="email">Email пользователя (если null, генерируется автоматически).</param>
    /// <param name="password">Пароль пользователя.</param>
    /// <returns>Кортеж (идентификатор пользователя, email).</returns>
    public static async Task<(UserIdentification userId, string email)> CreateTestUserWithEmailAsync(
        IServiceProvider serviceProvider,
        string? email = null,
        string password = "Password123!")
    {
        var userManager = serviceProvider.GetRequiredService<JoinUserManager>();
        email ??= $"test-{Guid.NewGuid()}@example.com";
        
        var user = new JoinIdentityUser { UserName = email };
        await userManager.CreateAsync(user, password);
        
        user = await userManager.FindByNameAsync(email)
            ?? throw new InvalidOperationException("User not found");
        
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await userManager.ConfirmEmailAsync(user, token);
        
        return (new UserIdentification(user.Id), email);
    }

    /// <summary>
    /// Создаёт проект от имени указанного пользователя.
    /// </summary>
    /// <param name="serviceProvider">Service provider для получения зависимостей.</param>
    /// <param name="userId">Идентификатор пользователя-владельца проекта.</param>
    /// <param name="projectName">Название проекта.</param>
    /// <param name="projectType">Тип проекта (по умолчанию LARP).</param>
    /// <returns>Идентификатор созданного проекта.</returns>
    public static async Task<ProjectIdentification> CreateProjectAsync(
        IServiceProvider serviceProvider,
        UserIdentification userId,
        string projectName = "Тестовый проект",
        ProjectTypeDto projectType = ProjectTypeDto.Larp)
    {
        var impersonator = serviceProvider.GetRequiredService<IImpersonateAccessor>();
        impersonator.StartImpersonate(userId, new UserDisplayName("Тестовый Мастер", null), IsAdmin: false);
        
        try
        {
            var createProjectService = serviceProvider.GetRequiredService<ICreateProjectService>();
            var result = await createProjectService.CreateProject(
                CreateProjectRequest.Create(
                    new ProjectName(projectName),
                    projectType,
                    null,
                    default));
            
            return result switch
            {
                SuccessCreateProjectResult r => r.ProjectId,
                PartiallySuccessCreateProjectResult r => r.ProjectId,
                _ => throw new InvalidOperationException($"Failed to create project: {result}")
            };
        }
        finally
        {
            impersonator.StopImpersonate();
        }
    }

    /// <summary>
    /// Аутентифицирует HTTP-клиент под указанным пользователем.
    /// </summary>
    /// <param name="client">HTTP-клиент (без аутентификации).</param>
    /// <param name="email">Email пользователя.</param>
    /// <param name="password">Пароль пользователя.</param>
    /// <returns>Тот же HTTP-клиент с установленными аутентификационными cookie.</returns>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        HttpClient client,
        string email,
        string password = "Password123!")
    {
        // Получить страницу входа для antiforgery токена
        var loginGetResponse = await client.GetAsync("account/login");
        loginGetResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var loginDoc = await loginGetResponse.AsHtmlDocument();
        var antiforgeryToken = loginDoc.DocumentNode
            .SelectSingleNode("//input[@name='__RequestVerificationToken']")?
            .GetAttributeValue("value", "")
            ?? throw new InvalidOperationException("Antiforgery token not found");
        
        // Выполнить вход
        var loginPostResponse = await client.PostAsync("account/login",
            new FormUrlEncodedContent(
                new[]
                {
                    new KeyValuePair<string?, string?>("Email", email),
                    new KeyValuePair<string?, string?>("Password", password),
                    new KeyValuePair<string?, string?>("__RequestVerificationToken", antiforgeryToken),
                }));
        
        // Вход должен быть успешным
        loginPostResponse.IsSuccessStatusCode.ShouldBeTrue();
        
        return client;
    }
}