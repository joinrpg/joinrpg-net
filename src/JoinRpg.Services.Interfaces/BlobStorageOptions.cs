#nullable enable

namespace JoinRpg.Services.Interfaces;

/// <summary>
/// Options about avatar
/// </summary>
public class BlobStorageOptions
{
    /// <summary>
    /// Connections string to blob storage
    /// </summary>
    public string BlobStorageConnectionString { get; set; } = null!;
    /// <summary>
    /// Disable/enable
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Avatar storage enabled
    /// </summary>
    public bool BlobStorageConfigured => !string.IsNullOrWhiteSpace(BlobStorageConnectionString) && Enabled;
}
