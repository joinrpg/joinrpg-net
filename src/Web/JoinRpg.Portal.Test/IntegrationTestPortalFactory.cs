using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test;

public sealed class IntegrationTestPortalFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Окружение не Development, поэтому http-запросы редиректятся на https (см. ForwardedHeadersExtensions
    /// в WebInfrastructure). Клиенты ходят сразу по https, как браузер на проде.
    /// </summary>
    private static readonly Uri HttpsBaseAddress = new("https://localhost");

    public IntegrationTestPortalFactory() => ClientOptions.BaseAddress = HttpsBaseAddress;

    /// <summary>
    /// Клиент, который не ходит по редиректам — чтобы проверять сами редиректы приложения.
    /// </summary>
    /// <remarks>
    /// Свои <see cref="WebApplicationFactoryClientOptions"/> не наследуют <see cref="WebApplicationFactory{TEntryPoint}.ClientOptions"/>,
    /// поэтому https-адрес задаём и здесь, иначе вместо ответа приложения придёт редирект на https.
    /// </remarks>
    public HttpClient CreateClientWithoutRedirects()
        => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = HttpsBaseAddress });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
    }
}
