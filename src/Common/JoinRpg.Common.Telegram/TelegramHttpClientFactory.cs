using System.Net;

namespace JoinRpg.Common.Telegram;

public static class TelegramHttpClientFactory
{
    public static HttpClient? Create(TelegramProxyOptions? options)
    {
        if (options is null)
        {
            return null;
        }

        var webProxy = new WebProxy(new Uri(options.Address));
        if (!string.IsNullOrEmpty(options.Username))
        {
            webProxy.Credentials = new NetworkCredential(options.Username, options.Password);
        }

        return new HttpClient(new HttpClientHandler { Proxy = webProxy, UseProxy = true });
    }
}
