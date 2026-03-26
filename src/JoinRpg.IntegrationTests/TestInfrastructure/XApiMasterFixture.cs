using System.Net.Http.Headers;
using Joinrpg.Web.Identity;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Shared fixture for XApi tests. Creates a master user, a project, and a JWT token.
/// Reused across all tests in the XApi collection.
/// </summary>
public class XApiMasterFixture : IAsyncLifetime
{
    public const string MasterEmail = "master@integrationtest.joinrpg.ru";
    public const string MasterPassword = "TestPassword123!";
    public const string MasterDisplayName = "Мастер Тестовый";

    public JoinApplicationFactory Factory { get; } = new();

    public int ProjectId { get; private set; }

    public XApiClient MasterClient { get; private set; } = null!;

    public HttpClient AnonymousClient => Factory.CreateClient();

    public XApiClient AnonymousXApiClient => new XApiClient(Factory.CreateClient());

    public XApiClient CreateAuthorizedXApiClient(string token)
    {
        var httpClient = Factory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new XApiClient(httpClient);
    }

    public async Task InitializeAsync()
    {
        await ((IAsyncLifetime)Factory).InitializeAsync();

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<JoinUserManager>();

        // Create master user
        var user = new JoinIdentityUser { UserName = MasterEmail };
        await userManager.CreateAsync(user, MasterPassword);
        user = await userManager.FindByNameAsync(MasterEmail)
            ?? throw new InvalidOperationException("Master user not found after creation");

        // Confirm email so user can log in
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await userManager.ConfirmEmailAsync(user, token);

        var userId = new UserIdentification(user.Id);
        var displayName = new UserDisplayName(MasterDisplayName, null);

        // Create project as master user via service
        var impersonator = scope.ServiceProvider.GetRequiredService<IImpersonateAccessor>();
        impersonator.StartImpersonate(userId, displayName, IsAdmin: false);
        try
        {
            var createProjectService = scope.ServiceProvider.GetRequiredService<ICreateProjectService>();
            var result = await createProjectService.CreateProject(
                CreateProjectRequest.Create(
                    new ProjectName("Тестовая Игра"),
                    ProjectTypeDto.EmptyProject,
                    null,
                    default));

            ProjectId = result switch
            {
                SuccessCreateProjectResult r => r.ProjectId.Value,
                PartiallySuccessCreateProjectResult r => r.ProjectId.Value,
                _ => throw new InvalidOperationException($"Failed to create project: {result}"),
            };
        }
        finally
        {
            impersonator.StopImpersonate();
        }

        // Get JWT token
        var anonymousClient = Factory.CreateClient();
        var anonymousXApi = new XApiClient(anonymousClient);
        var authResponse = await anonymousXApi.LoginAsync(MasterEmail, MasterPassword);

        MasterClient = CreateAuthorizedXApiClient(authResponse.access_token);
    }

    public async Task DisposeAsync()
    {
        await ((IAsyncLifetime)Factory).DisposeAsync();
    }
}
