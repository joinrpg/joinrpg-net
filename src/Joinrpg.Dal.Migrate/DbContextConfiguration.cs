using JoinRpg.Dal.Impl;

namespace Joinrpg.Dal.Migrate;

/// <inheritdoc />
public class DbContextConfiguration : IJoinDbContextConfiguration
{
    /// <inheritdoc />
    public string ConnectionString { get; set; }

    /// <inheritdoc />
    public bool DetailedErrors { get; set; }

    /// <inheritdoc />
    public bool SensitiveLogging { get; set; }
}
