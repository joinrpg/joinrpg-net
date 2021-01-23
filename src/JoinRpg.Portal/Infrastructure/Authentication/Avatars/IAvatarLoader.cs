using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Helpers.Web;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Portal.Infrastructure.Authentication.Avatars
{
#nullable enable
    public interface IAvatarLoader
    {
        Task<AvatarInfo> GetAvatar(AvatarIdentification userAvatarId, int recommendedSize);
    }

    internal class AvatarCacheDecoractor : IAvatarLoader
    {
        private readonly IAvatarLoader innerLoader;
        private record Key(AvatarIdentification Id, int Size) { }
        private static readonly ConcurrentDictionary<Key, Task<AvatarInfo>> Cache = new();

        public AvatarCacheDecoractor(IAvatarLoader innerLoader) => this.innerLoader = innerLoader;
        Task<AvatarInfo> IAvatarLoader.GetAvatar(AvatarIdentification userAvatarId, int recommendedSize)
        {
            var key = new Key(userAvatarId, recommendedSize);
            var cachedTask = Cache.GetOrAdd(key, k => Load(k));

            if (cachedTask.IsFaulted)
            {
                //cachedtask is memoized failure. Let's restart (once).
                var restart = Load(key);
                _ = Cache.TryUpdate(key, restart, cachedTask);
                return restart;
            }
            else
            {
                return cachedTask;
            }

            Task<AvatarInfo> Load(Key key) => innerLoader.GetAvatar(key.Id, key.Size);
        }

    }

    internal class AvatarLoader : IAvatarLoader
    {
        private readonly IUserRepository userRepository;

        public AvatarLoader(IUserRepository userRepository) => this.userRepository = userRepository;
        public async Task<AvatarInfo> GetAvatar(AvatarIdentification userAvatarId, int recommendedSize)
        {
            var avatar = await userRepository.LoadAvatar(userAvatarId);
            return new AvatarInfo(new Uri(avatar.Uri), recommendedSize);
        }
    }
}
