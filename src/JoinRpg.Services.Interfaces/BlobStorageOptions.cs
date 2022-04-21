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
    /// Avatar storage enabled
    /// </summary>
    public bool BlobStorageConfigured => !string.IsNullOrWhiteSpace(BlobStorageConnectionString);
}
