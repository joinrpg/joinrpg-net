using JoinRpg.Common.WebInfrastructure.DataProtection;
using JoinRpg.Common.WebInfrastructure.EfCoreMigration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Без строки подключения мигратор раньше регистрировался без DbContext и падал при запуске
/// невнятной ошибкой DI — так выглядел запуск Joinrpg.Dal.Migrate не из каталога проекта.
/// </summary>
public class RegisterMigratorTest
{
    private static readonly IHostEnvironment Environment = new HostingEnvironment { EnvironmentName = Environments.Development };

    [Fact]
    public void MissingConnectionStringFailsWithClearMessage()
    {
        var configuration = new ConfigurationBuilder().Build();

        var exception = Should.Throw<InvalidOperationException>(
            () => new ServiceCollection().RegisterMigrator<DataProtectionDbContext>(configuration, Environment, "DataProtection"));

        exception.Message.ShouldContain("ConnectionStrings:DataProtection");
    }

    [Fact]
    public void WithConnectionStringRegistersMigratorAndContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DataProtection"] = "Host=localhost;database=x;username=x;password=x",
            })
            .Build();
        var services = new ServiceCollection();

        services.RegisterMigrator<DataProtectionDbContext>(configuration, Environment, "DataProtection");

        services.ShouldContain(d => d.ServiceType == typeof(IMigratorService));
        services.ShouldContain(d => d.ServiceType == typeof(DataProtectionDbContext));
    }
}
