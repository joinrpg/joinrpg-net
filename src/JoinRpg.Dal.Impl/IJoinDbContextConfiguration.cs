using Microsoft.EntityFrameworkCore;

namespace JoinRpg.Dal.Impl;

/// <summary>
/// Provides configuration for DbContext
/// </summary>
public interface IJoinDbContextConfiguration
{
    /// <summary>
    /// Connection String
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// Enables <see cref="DbContextOptionsBuilder.EnableDetailedErrors"/>
    /// </summary>
    bool DetailedErrors { get; }

    /// <summary>
    /// Enables <see cref="DbContextOptionsBuilder.EnableSensitiveDataLogging"/>
    /// </summary>
    bool SensitiveLogging { get; }
}
