using System.Runtime.CompilerServices;
using Joinrpg.Web.Identity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Interfaces;
using JoinRpg.Portal;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JoinRpg.Tools.SeedTestUsers;

/// <summary>
/// Локальный dev-инструмент: создаёт тестовых пользователей и тестовый проект в локальной БД.
/// Не является частью JoinRpg.Portal и не разворачивается вместе с ним — запускается вручную:
/// dotnet run --project src/JoinRpg.Tools.SeedTestUsers
/// Подробности — docs/local-dev-test-users.md.
/// </summary>
internal static class Program
{
    private const string Password = "Test12345!";
    private const string AdminEmail = "admin@example.com";
    private const string MasterEmail = "master@example.com";
    private const string PlayerEmail = "player@example.com";
    private const string TestProjectName = "Тестовая песочница";

    private static async Task<int> Main()
    {
        Console.WriteLine("JoinRpg.Tools.SeedTestUsers: создание тестовых пользователей для локальной разработки.");

        await using var factory = new WebApplicationFactory<Startup>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseContentRoot(GetPortalContentRoot());
                builder.ConfigureTestServices(services => services.RemoveAll<IHostedService>());
            });

        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        if (!LooksLikeLocalDatabase(sp, out var connectionString))
        {
            Console.Error.WriteLine(
                $"Строка подключения не похожа на локальную БД (нет localhost/127.0.0.1): {connectionString}");
            Console.Error.WriteLine("Прерываю, чтобы случайно не создать тестовых пользователей на чужой БД.");
            return 1;
        }

        var userManager = sp.GetRequiredService<JoinUserManager>();
        if (await userManager.FindByNameAsync(MasterEmail) is not null)
        {
            Console.WriteLine($"Пользователь {MasterEmail} уже существует — тестовые данные уже засеяны, ничего не делаю.");
            return 0;
        }

        // Порядок важен: первый созданный в пустой БД пользователь автоматически становится
        // сайт-админом (см. MyUserStore.Common.CreateImpl), поэтому создаём admin@example.com первым.
        var adminId = await CreateUserAsync(userManager, AdminEmail);
        await EnsureSiteAdminAsync(sp, adminId);

        var masterId = await CreateUserAsync(userManager, MasterEmail);
        await CreateUserAsync(userManager, PlayerEmail);

        var projectId = await CreateProjectAsync(sp, masterId);

        Console.WriteLine();
        Console.WriteLine("Готово. Созданы тестовые пользователи (пароль у всех: " + Password + "):");
        Console.WriteLine($"  {AdminEmail} — администратор сайта");
        Console.WriteLine($"  {MasterEmail} — владелец тестового проекта «{TestProjectName}» (#{projectId.Value})");
        Console.WriteLine($"  {PlayerEmail} — обычный пользователь без прав на проект");
        Console.WriteLine("Подробности — docs/local-dev-test-users.md");

        return 0;
    }

    private static string GetPortalContentRoot([CallerFilePath] string sourceFilePath = "")
    {
        // WebApplicationFactory не умеет находить content root Portal при UseArtifactsOutput —
        // берём его напрямую из расположения этого файла в дереве исходников.
        var srcDir = Path.GetDirectoryName(Path.GetDirectoryName(sourceFilePath))!;
        return Path.Combine(srcDir, "JoinRpg.Portal");
    }

    private static bool LooksLikeLocalDatabase(IServiceProvider sp, out string connectionString)
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        return connectionString.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<UserIdentification> CreateUserAsync(JoinUserManager userManager, string email)
    {
        var identityUser = new JoinIdentityUser { UserName = email };
        var createResult = await userManager.CreateAsync(identityUser, Password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Не удалось создать тестового пользователя {email}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
        }

        identityUser = await userManager.FindByNameAsync(email)
            ?? throw new InvalidOperationException($"Не удалось создать тестового пользователя {email}");

        var token = await userManager.GenerateEmailConfirmationTokenAsync(identityUser);
        _ = await userManager.ConfirmEmailAsync(identityUser, token);

        Console.WriteLine($"  создан пользователь {email}");

        return new UserIdentification(identityUser.Id);
    }

    private static async Task EnsureSiteAdminAsync(IServiceProvider sp, UserIdentification userId)
    {
        var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
        var user = unitOfWork.GetDbSet<User>().Find(userId.Value)
            ?? throw new InvalidOperationException($"Тестовый пользователь #{userId.Value} не найден");
        user.Auth.IsAdmin = true;
        await unitOfWork.SaveChangesAsync();
    }

    private static async Task<ProjectIdentification> CreateProjectAsync(IServiceProvider sp, UserIdentification masterId)
    {
        var impersonator = sp.GetRequiredService<IImpersonateAccessor>();
        impersonator.StartImpersonate(masterId, new UserDisplayName("Тестовый Мастер", null), IsAdmin: false);
        try
        {
            var createProjectService = sp.GetRequiredService<ICreateProjectService>();
            var result = await createProjectService.CreateProject(
                CreateProjectRequest.Create(
                    new ProjectName(TestProjectName),
                    ProjectTypeDto.Larp,
                    null,
                    default,
                    KogdaIgraLinkChoiceDto.ShouldNotBeOnKogdaIgra,
                    null,
                    null));

            var projectId = result switch
            {
                SuccessCreateProjectResult r => r.ProjectId,
                PartiallySuccessCreateProjectResult r => r.ProjectId,
                _ => throw new InvalidOperationException($"Не удалось создать тестовый проект: {result}"),
            };

            Console.WriteLine($"  создан проект «{TestProjectName}» (#{projectId.Value})");

            return projectId;
        }
        finally
        {
            impersonator.StopImpersonate();
        }
    }
}
