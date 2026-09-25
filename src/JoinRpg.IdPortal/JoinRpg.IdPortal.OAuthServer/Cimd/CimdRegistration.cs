using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.IdPortal.OAuthServer.Cimd;

public static class CimdRegistration
{
    /// <summary>Имя параметра метаданных AS, которым сервер объявляет поддержку CIMD (§5 драфта).</summary>
    public const string SupportedMetadataParameter = "client_id_metadata_document_supported";

    public static IServiceCollection AddCimdSupport(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddHttpClient<ICimdMetadataLoader, CimdMetadataLoader>(CimdMetadataLoader.ConfigureHttpClient)
            // Редиректы выключены намеренно: следовать за ними — это обход проверки адреса,
            // публичный URL мог бы увести на 127.0.0.1 или в облачную метадату.
            .ConfigurePrimaryHttpMessageHandler(CimdMetadataLoader.CreateHandler);

        return services;
    }
}
