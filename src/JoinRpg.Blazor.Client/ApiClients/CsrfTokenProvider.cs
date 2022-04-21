using Microsoft.JSInterop;

namespace JoinRpg.Blazor.Client.ApiClients;

public class CsrfTokenProvider
{
    private readonly IJSRuntime jsRuntime;

    public CsrfTokenProvider(IJSRuntime jsRuntime) => this.jsRuntime = jsRuntime;

    private class StringHolder { public string Content { get; set; } = null!; }

    private string? CachedToken = null;

    private async ValueTask<string> GetCsrfTokenAsync()
    {
        if (CachedToken is null)
        {
            CachedToken = await GetCsrfTokenAsyncFromJs();
            if (CachedToken is null)
            {
                throw new Exception("Can't get CSRF token");
            }
        }
        return CachedToken;
    }
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

    public async Task SetCsrfToken(HttpClient httpClient) => httpClient.DefaultRequestHeaders.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync());
}
