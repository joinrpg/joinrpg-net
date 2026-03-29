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
    internal const string MasterEmail = "master@integrationtest.joinrpg.ru";
    internal const string MasterPassword = "TestPassword123!";
    private const string MasterDisplayName = "Мастер Тестовый";

    public JoinApplicationFactory Factory { get; } = new();

    public int ProjectId { get; private set; }

    public XApiClient MasterClient { get; private set; } = null!;

    public XApiClient AnonymousXApiClient => new XApiClient(Factory.CreateClient());

    public async Task InitializeAsync()
    {
        await ((IAsyncLifetime)Factory).InitializeAsync();

        UserIdentification userId = await CreateMasterUser();
        var displayName = new UserDisplayName(MasterDisplayName, null);

        ProjectId = await CreateNewProject(userId, displayName);


        var httpClient = Factory.CreateClient();
        MasterClient = await XApiClient.CreateXApiClient(httpClient, MasterEmail, MasterPassword);
    }

    private async Task<UserIdentification> CreateMasterUser()
    {
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

        return new UserIdentification(user.Id);
    }

    private async Task<ProjectIdentification> CreateNewProject(UserIdentification userId, UserDisplayName displayName)
    {
        using var scope2 = Factory.Services.CreateScope();
        // Create project as master user via service
        var impersonator = scope2.ServiceProvider.GetRequiredService<IImpersonateAccessor>();
        impersonator.StartImpersonate(userId, displayName, IsAdmin: false);
        try
        {
            var createProjectService = scope2.ServiceProvider.GetRequiredService<ICreateProjectService>();
            var result = await createProjectService.CreateProject(
                CreateProjectRequest.Create(
                    new ProjectName("Тестовая Игра"),
                    ProjectTypeDto.Larp,
                    null,
                    default));

            return result switch
            {
                SuccessCreateProjectResult r => r.ProjectId,
                PartiallySuccessCreateProjectResult r => r.ProjectId,
                _ => throw new InvalidOperationException($"Failed to create project: {result}"),
            };
        }
        finally
        {
            impersonator.StopImpersonate();
        }
    }

    public async Task DisposeAsync()
    {
        await ((IAsyncLifetime)Factory).DisposeAsync();
    }
}
