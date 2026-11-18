using JoinRpg.Common.WebInfrastructure.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Миграции DataProtectionDbContext не входят в публичный API пакета, но EF должен их находить и
/// применять.
/// </summary>
public class DataProtectionMigrationsTest
{
    [Fact]
    public void InternalMigrationIsApplied()
    {
        var options = new DbContextOptionsBuilder<DataProtectionDbContext>()
            .UseNpgsql("Host=localhost;database=x;username=x;password=x")
            .Options;
        using var context = new DataProtectionDbContext(options);

        context.Database.GetMigrations().ShouldContain("20220502193939_Initial");
        // Скрипт строится тем же путём, что Migrate(): экземпляр миграции и её Up.
        context.GetService<IMigrator>().GenerateScript().ShouldContain("DataProtectionKeys");
    }
}
