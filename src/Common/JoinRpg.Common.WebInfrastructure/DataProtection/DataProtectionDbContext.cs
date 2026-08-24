using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JoinRpg.Common.WebInfrastructure.DataProtection;

public class DataProtectionDbContext(DbContextOptions<DataProtectionDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
}
