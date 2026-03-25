using System.Net.Http.Headers;
using Joinrpg.Web.Identity;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Shared test fixture: creates a master user and a project, obtains a JWT token.
/// Use via <see cref="XApiCollection"/> to reuse across all XApi tests.
/// </summary>
public class XApiMasterFixture : IAsyncLifetime
{
    public const string MasterEmail = "master@xapi-test.ru";
    public const string MasterPassword = "MasterPassword123!";
    public const string ProjectName = "XApi Integration Test Project";

    public JoinApplicationFactory Factory { get; } = new();
    public int ProjectId { get; private set; }

    /// <summary>HttpClient with master's Bearer token pre-configured.</summary>
    public XApiClient AuthorizedClient { get; private set; } = null!;

    /// <summary>Unauthenticated client for testing 401 responses.</summary>
    public XApiClient AnonymousClient { get; private set; } = null!;

    async Task IAsyncLifetime.InitializeAsync()
    {
        await ((IAsyncLifetime)Factory).InitializeAsync();

        using var scope = Factory.Services.CreateScope();

        // Create master user
        var userManager = scope.ServiceProvider.GetRequiredService<JoinUserManager>();
        var user = new JoinIdentityUser { UserName = MasterEmail, DisplayName = new UserDisplayName("Мастер Тест", null) };
        var result = await userManager.CreateAsync(user, MasterPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create master user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        user = await userManager.FindByNameAsync(MasterEmail)
            ?? throw new InvalidOperationException("Master user not found after creation");
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await userManager.ConfirmEmailAsync(user, token);

        // Create project via service (impersonating master user)
        var impersonateAccessor = scope.ServiceProvider.GetRequiredService<IImpersonateAccessor>();
        var userId = new UserIdentification(user.Id);
        impersonateAccessor.StartImpersonate(userId, user.DisplayName!, false);
        try
        {
            var createProjectService = scope.ServiceProvider.GetRequiredService<ICreateProjectService>();
            var createResult = await createProjectService.CreateProject(
                CreateProjectRequest.Create(
                    new ProjectName(ProjectName),
                    ProjectTypeDto.Larp,
                    CopyFromId: null,
                    CopySettings: ProjectCopySettingsDto.SettingsAndFields));

            ProjectId = createResult switch
            {
                SuccessCreateProjectResult success => success.ProjectId.Value,
                PartiallySuccessCreateProjectResult partial => partial.ProjectId.Value,
                _ => throw new InvalidOperationException($"Failed to create project: {createResult}"),
            };
        }
        finally
        {
            impersonateAccessor.StopImpersonate();
        }

        // Obtain JWT token
        var anonymousHttpClient = Factory.CreateClient();
        var tokenResponse = await anonymousHttpClient.PostAsync("x-api/token",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string?, string?>("username", MasterEmail),
                new KeyValuePair<string?, string?>("password", MasterPassword),
                new KeyValuePair<string?, string?>("grant_type", "password"),
            ]));
        tokenResponse.EnsureSuccessStatusCode();
        var authResponse = await tokenResponse.Content.ReadFromJsonAsync<JoinRpg.XGameApi.Contract.AuthenticationResponse>()
            ?? throw new InvalidOperationException("Failed to parse token response");

        // Build authorized HttpClient
        var authorizedHttpClient = Factory.CreateClient();
        authorizedHttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResponse.access_token);
        AuthorizedClient = new XApiClient(authorizedHttpClient);

        AnonymousClient = new XApiClient(Factory.CreateClient());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await ((IAsyncLifetime)Factory).DisposeAsync();
    }
}

[CollectionDefinition("XApi")]
public class XApiCollection : ICollectionFixture<XApiMasterFixture>;
