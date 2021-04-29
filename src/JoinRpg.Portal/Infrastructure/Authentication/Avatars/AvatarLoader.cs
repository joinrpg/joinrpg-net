using System;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Portal.Infrastructure.Authentication.Avatars
{
#nullable enable
    internal class AvatarLoader : IAvatarLoader
    {
        private readonly IUserRepository userRepository;

        public AvatarLoader(IUserRepository userRepository) => this.userRepository = userRepository;
        public async Task<AvatarInfo> GetAvatar(AvatarIdentification userAvatarId, int recommendedSize)
        {
            var avatar = await userRepository.LoadAvatar(userAvatarId);
            return new AvatarInfo(new Uri(avatar.CachedUri ?? avatar.OriginalUri), recommendedSize);
        }
    }
}
