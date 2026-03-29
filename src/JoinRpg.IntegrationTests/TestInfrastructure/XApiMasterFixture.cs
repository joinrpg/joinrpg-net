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
    private readonly UserDisplayName MasterDisplayName = new("Мастер Тестовый", FullName: null);

    public JoinApplicationFactory Factory { get; } = new();

    public ProjectIdentification ProjectId { get; private set; } = null!;

    public UserIdentification MasterUserId { get; private set; } = null!;

    public XApiClient MasterClient { get; private set; } = null!;

    public XApiClient AnonymousXApiClient => new XApiClient(Factory.CreateClient());

    public async Task InitializeAsync()
    {
        await ((IAsyncLifetime)Factory).InitializeAsync();

        MasterUserId = await CreateMasterUser();
        ProjectId = await CreateNewProject(MasterUserId);


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

    internal async Task<ProjectIdentification> CreateNewProject(UserIdentification userId, ProjectTypeDto projectType = ProjectTypeDto.Larp)
    {
        using var scope2 = Factory.Services.CreateScope();
        // Create project as master user via service
        var impersonator = scope2.ServiceProvider.GetRequiredService<IImpersonateAccessor>();

        var displayName = MasterDisplayName; // Load from db by UserId
        impersonator.StartImpersonate(userId, displayName, IsAdmin: false);
        try
        {
            var createProjectService = scope2.ServiceProvider.GetRequiredService<ICreateProjectService>();
            var result = await createProjectService.CreateProject(
                CreateProjectRequest.Create(
                    new ProjectName("Тестовая Игра"),
                    projectType,
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

    
    internal async Task<ProjectFieldIdentification> CreateField(
        UserIdentification userId,
        ProjectIdentification projectId,
        ProjectFieldType fieldType,
        string name)
    {
        using var scope = Factory.Services.CreateScope();
        var impersonator = scope.ServiceProvider.GetRequiredService<IImpersonateAccessor>();
        impersonator.StartImpersonate(userId, MasterDisplayName, IsAdmin: false);
        try
        {
            var fieldSetupService = scope.ServiceProvider.GetRequiredService<IFieldSetupService>();
            return await fieldSetupService.AddField(new CreateFieldRequest(
                projectId,
                fieldType,
                name,
                fieldHint: "",
                canPlayerEdit: false,
                canPlayerView: true,
                isPublic: true,
                FieldBoundTo.Character,
                MandatoryStatus.Optional,
                [],
                validForNpc: true,
                includeInPrint: true,
                showForUnapprovedClaims: false,
                price: 0,
                masterFieldHint: "",
                programmaticValue: null));
        }
        finally
        {
            impersonator.StopImpersonate();
        }
    }

    internal async Task<int> CreateFieldVariant(
        UserIdentification userId,
        ProjectFieldIdentification fieldId,
        string label)
    {
        using var scope = Factory.Services.CreateScope();
        var impersonator = scope.ServiceProvider.GetRequiredService<IImpersonateAccessor>();
        impersonator.StartImpersonate(userId, MasterDisplayName, IsAdmin: false);
        try
        {
            var fieldSetupService = scope.ServiceProvider.GetRequiredService<IFieldSetupService>();
            var variantId = await fieldSetupService.CreateFieldValueVariant(new CreateFieldValueVariantRequest(
                fieldId,
                label,
                description: null,
                masterDescription: null,
                programmaticValue: null,
                price: 0,
                playerSelectable: true,
                timeSlotOptions: null));
            return variantId.ProjectFieldVariantId;
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
