using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Приложение с одной базой хранит ключи Data Protection в своём DbContext, без отдельной строки подключения.
/// </summary>
public class DataProtectionRegistrationTest
{
    private class AppKeysContext(DbContextOptions<AppKeysContext> options) : DbContext(options), IDataProtectionKeyContext
    {
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    }

    [Fact]
    public void KeysArePersistedToApplicationContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppKeysContext>(options => options.UseNpgsql("Host=localhost;database=x;username=x;password=x"));

        services.AddJoinDataProtection<AppKeysContext>("MyApp");

        using var provider = services.BuildServiceProvider();
        var keyManagement = provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value;
        keyManagement.XmlRepository.ShouldBeOfType<EntityFrameworkCoreXmlRepository<AppKeysContext>>();
        provider.GetRequiredService<IOptions<DataProtectionOptions>>().Value.ApplicationDiscriminator.ShouldBe("MyApp");
    }
}
