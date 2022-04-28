using Autofac;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.BlobStorage;

/// <summary>
/// Module that select correct avatar storage
/// </summary>
public class BlobStorageModule : Module
{
    private readonly BlobStorageOptions? options;
    private readonly S3StorageOptions? s3Options;

    /// <summary>
    /// ctor
    /// </summary>
    public BlobStorageModule(BlobStorageOptions? options, S3StorageOptions? s3Options)
    {
        this.options = options;
        this.s3Options = s3Options;
    }

    /// <inheritdoc/>
    protected override void Load(ContainerBuilder builder)
    {
        _ = builder.RegisterType<AzureBlobStorageConnectionFactory>().AsSelf();
        _ = builder.RegisterType<AvatarDownloader>().AsSelf();
        if (options?.BlobStorageConfigured == true)
        {
            _ = builder.RegisterType<AzureAvatarStorageService>().As<IAvatarStorageService>();

        }
        else if (s3Options?.Configured == true)
        {
            _ = builder.Register(c => S3ClientFactory.CreateS3Client(s3Options)).SingleInstance();
            _ = builder.RegisterType<S3AvatarStorageService>().As<IAvatarStorageService>();
        }
        else
        {
            _ = builder.RegisterType<StubAvatarStorageService>().As<IAvatarStorageService>();
        }
    }
}
