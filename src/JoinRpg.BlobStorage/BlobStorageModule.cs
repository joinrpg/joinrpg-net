using Autofac;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.BlobStorage
{
    /// <summary>
    /// Module that select correct avatar storage
    /// </summary>
    public class BlobStorageModule : Module
    {
        private readonly BlobStorageOptions? options;

        /// <summary>
        /// ctor
        /// </summary>
        public BlobStorageModule(BlobStorageOptions? options) => this.options = options;

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder)
        {
            _ = builder.RegisterType<AzureBlobStorageConnectionFactory>().AsSelf();
            if (options?.BlobStorageConfigured == true)
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
