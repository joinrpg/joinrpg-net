using Microsoft.JSInterop;

namespace JoinRpg.Blazor.Client.ApiClients;

public class CsrfTokenProvider
{
    private readonly IJSRuntime jsRuntime;

    public CsrfTokenProvider(IJSRuntime jsRuntime) => this.jsRuntime = jsRuntime;

    private class StringHolder { public string Content { get; set; } = null!; }

    /// <remarks>
    /// Токен читается из cookie на каждый запрос, а не кешируется: после перелогина в соседней
    /// вкладке cookie меняется, и кешированный токен отвергался бы до перезагрузки страницы.
    /// </remarks>
    private async ValueTask<string> GetCsrfTokenAsync()
        => await GetCsrfTokenAsyncFromJs() ?? throw new Exception("Can't get CSRF token");
    private async ValueTask<string?> GetCsrfTokenAsyncFromJs()
    {
        await using var module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "/Scripts/blazor-interop.js");

        var cookies = await module.InvokeAsync<StringHolder>("getDocumentCookie");
        return cookies
            .Content
            .Split(';')
            .Select(v => v.TrimStart().Split('='))
            .Where(s => s[0] == "CSRF-TOKEN")
            .Select(s => s[1])
            .FirstOrDefault();
    }

    /// <summary>
    /// Ставит токен в заголовки клиента. Заменяет, а не добавляет: типизированный клиент живёт
    /// столько же, сколько компонент, и со второго POST заголовок ушёл бы как «tok, tok» — такой
    /// antiforgery не принимает.
    /// </summary>
    public async Task SetCsrfToken(HttpClient httpClient)
    {
        var token = await GetCsrfTokenAsync();
        _ = httpClient.DefaultRequestHeaders.Remove(HeaderName);
        httpClient.DefaultRequestHeaders.Add(HeaderName, token);
    }

    private const string HeaderName = "X-CSRF-TOKEN";
}
