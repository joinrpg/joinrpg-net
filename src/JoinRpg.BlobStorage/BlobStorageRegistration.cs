using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.BlobStorage;

public static class BlobStorageRegistration
{
    public static IServiceCollection AddJoinBlobStorage(this IServiceCollection services)
    {
        _ = services
            .AddHttpClient(AvatarDownloader.HttpClientName, client => client.Timeout = AvatarDownloader.DownloadTimeout);

        return services
            .AddHttpClient()
            .AddTransient<AvatarDownloader>()
            .AddTransient<IAvatarStorageService>(sp =>
        {
            var s3Options = sp.GetRequiredService<IOptions<S3StorageOptions>>();
            if (s3Options.Value.Configured)
            {
                return sp.GetRequiredService<S3AvatarStorageService>();
            }
            else
            {
                return sp.GetRequiredService<StubAvatarStorageService>();
            }
        })
            .AddTransient<S3AvatarStorageService>()
            .AddTransient<StubAvatarStorageService>()
            .AddSingleton<IAvatarUriResolver, AvatarUriResolver>();
    }
}
