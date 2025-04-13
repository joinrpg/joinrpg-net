
namespace JoinRpg.Portal.Infrastructure.Logging;

public class YandexLogLink(IHostEnvironment hostEnvironment)
{
    internal Uri GetLinkForUser(string email)
    {
        var env = hostEnvironment.IsProduction() ? "prod" : "dev";
        return new Uri($"https://console.yandex.cloud/folders/b1g1a4nj5oq5sv996tcv/logging/group/e2371utkp5mj3oltko4r/logs?from=now-1d&to=now&size=100&linesInRow=1&query=json_payload.kubernetes.namespace_name+%3D+{env}+and+LoggedUser+%3D+%22{email}%22+");
    }
}
