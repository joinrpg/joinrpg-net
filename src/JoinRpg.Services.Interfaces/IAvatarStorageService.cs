using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace JoinRpg.Services.Interfaces
{
    /// <summary>
    /// Service that stores avatars to permanent storage
    /// </summary>
    public interface IAvatarStorageService
    {
        /// <summary>
        /// Download avatar from remote URI and save to permanent storage
        /// </summary>
        /// <param name="remoteUri"></param>
        /// <param name="ct"></param>
        /// <returns>Uri of new permanent storage</returns>
        Task<Uri?> StoreAvatar(Uri remoteUri, CancellationToken ct = default);
    }

    /// <summary>
    /// Options about avatar
    /// </summary>
    public class AvatarStorageOptions
    {
        /// <summary>
        /// Connections string to blob storage
        /// </summary>
        public string AvatarStorageConnectionString { get; set; } = null!;

        /// <summary>
        /// Avatar storage enabled
        /// </summary>
        public bool AvatarStorageEnabled => !string.IsNullOrWhiteSpace(AvatarStorageConnectionString);
    }
}
