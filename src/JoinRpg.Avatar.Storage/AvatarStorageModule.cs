using Autofac;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Avatar.Storage
{
    /// <summary>
    /// Module that select correct avatar storage
    /// </summary>
    public class AvatarStorageModule : Module
    {
        private readonly AvatarStorageOptions options;

        /// <summary>
        /// ctor
        /// </summary>
        public AvatarStorageModule(AvatarStorageOptions options) => this.options = options;

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder)
        {
            if (options.AvatarStorageEnabled)
            {
                _ = builder.RegisterType<AzureAvatarStorageService>().As<IAvatarStorageService>();
            }
            else
            {
                _ = builder.RegisterType<StubAvatarStorageService>().As<IAvatarStorageService>();
            }
        }
    }
}
