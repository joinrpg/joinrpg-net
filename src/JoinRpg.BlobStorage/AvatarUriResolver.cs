using JoinRpg.Services.Interfaces;

namespace JoinRpg.BlobStorage;

internal class AvatarUriResolver(IOptions<S3StorageOptions> options) : IAvatarUriResolver
{
    public Uri GetAvatarUri(string? cachedUri, string originalUri) => Resolve(options.Value, cachedUri, originalUri);

    internal static Uri Resolve(S3StorageOptions options, string? cachedUri, string originalUri)
    {
        if (string.IsNullOrWhiteSpace(cachedUri))
        {
            return new Uri(originalUri);
        }

        // Старые записи (до перевода джобой) хранят полный адрес
        if (Uri.TryCreate(cachedUri, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        if (GetPublicBase(options) is not string publicBase)
        {
            return new Uri(originalUri);
        }

        return new Uri($"{publicBase.TrimEnd('/')}/{cachedUri.TrimStart('/')}");
    }

    private static string? GetPublicBase(S3StorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl))
        {
            return options.PublicBaseUrl;
        }
        if (!string.IsNullOrWhiteSpace(options.Endpoint) && !string.IsNullOrWhiteSpace(options.BucketName))
        {
            return $"{options.Endpoint.TrimEnd('/')}/{options.BucketName}";
        }
        return null;
    }
}
