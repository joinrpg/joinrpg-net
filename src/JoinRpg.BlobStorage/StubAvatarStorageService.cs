using System;
using System.Threading;
using System.Threading.Tasks;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace JoinRpg.BlobStorage
{
    internal class StubAvatarStorageService : IAvatarStorageService
    {
        private readonly ILogger<StubAvatarStorageService> logger;

        public StubAvatarStorageService(ILogger<StubAvatarStorageService> logger) => this.logger = logger;

        Task<Uri?> IAvatarStorageService.StoreAvatar(Uri remoteUri, CancellationToken ct)
        {
            logger.LogWarning("Avatar storage not configured. Request to cache avatar ignored");
            return Task.FromResult<Uri?>(null);
        }
    }
}
