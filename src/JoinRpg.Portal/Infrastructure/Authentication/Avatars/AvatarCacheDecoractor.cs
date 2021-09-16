using System.Collections.Concurrent;
using System.Threading.Tasks;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Portal.Infrastructure.Authentication.Avatars
{
#nullable enable
    /// <summary>
    /// Idea of this class to save trip to database to load URI of avatar.
    /// This cache cannot be too large (it just id to string), so we never purge it.
    /// Avatars are immutable.
    /// </summary>
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
}
