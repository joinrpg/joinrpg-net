using System.Net.Http.Headers;
using System.Net.Http.Json;
using Joinrpg.Web.Identity;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.XGameApi.Contract;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Shared fixture for XApi tests. Creates a master user and a project once per test collection.
/// </summary>
public class XApiMasterFixture : IAsyncLifetime
{
    public const string MasterEmail = "master@joinrpg-test.ru";
    public const string MasterPassword = "TestPassword1!";

    public JoinApplicationFactory Factory { get; } = new();
    public int ProjectId { get; private set; }

    /// <summary>HTTP client pre-configured with the master's Bearer token.</summary>
    public XApiClient AuthorizedClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await ((IAsyncLifetime)Factory).InitializeAsync();

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<JoinUserManager>();

        // Create master user (DisplayName has internal setter, set via the DB round-trip)
        var user = new JoinIdentityUser { UserName = MasterEmail };
        var createResult = await userManager.CreateAsync(user, MasterPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create master user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
        }

        user = await userManager.FindByNameAsync(MasterEmail)
            ?? throw new InvalidOperationException("Master user not found after creation");

        var confirmToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await userManager.ConfirmEmailAsync(user, confirmToken);

        // Create project via impersonation
        var impersonator = scope.ServiceProvider.GetRequiredService<IImpersonateAccessor>();
        var createProjectService = scope.ServiceProvider.GetRequiredService<ICreateProjectService>();

        // user.DisplayName is populated by the DB round-trip in FindByNameAsync
        var displayName = user.DisplayName ?? new UserDisplayName(MasterEmail, null);

        impersonator.StartImpersonate(new UserIdentification(user.Id), displayName, false);
        try
        {
            var projectResult = await createProjectService.CreateProject(
                CreateProjectRequest.Create(
                    new ProjectName("Тестовая Игра ИИ"),
                    ProjectTypeDto.Larp,
                    CopyFromId: null,
                    ProjectCopySettingsDto.SettingsAndFields));

            ProjectId = projectResult switch
            {
                SuccessCreateProjectResult s => s.ProjectId.Value,
                PartiallySuccessCreateProjectResult p => p.ProjectId.Value,
                _ => throw new InvalidOperationException($"Failed to create project: {projectResult}"),
            };
        }
        finally
        {
            impersonator.StopImpersonate();
        }

        // Obtain JWT token
        var anonClient = Factory.CreateClient();
        var loginResponse = await new XApiClient(anonClient)
            .PostLoginRawAsync(MasterEmail, MasterPassword);
        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>()
            ?? throw new InvalidOperationException("No auth token returned from /x-api/token");

        var authorizedHttpClient = Factory.CreateClient();
        authorizedHttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.access_token);
        AuthorizedClient = new XApiClient(authorizedHttpClient);
    }

    public async Task DisposeAsync()
    {
        await ((IAsyncLifetime)Factory).DisposeAsync();
        Factory.Dispose();
    }
}
