namespace JoinRpg.Blazor.Client.ApiClients;

internal static class HttpResponseProblemExtensions
{
    /// <summary>
    /// Как <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/>, но если сервер объяснил отказ в ProblemDetails,
    /// бросает <see cref="ServerProblemException"/> с его текстом — его можно показать пользователю.
    /// </summary>
    public static async Task EnsureSuccessOrThrowProblem(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }
        if (response.Content.Headers.ContentType?.MediaType == "application/problem+json"
            && await response.Content.ReadFromJsonAsync<ProblemResponse>() is { Title: string title })
        {
            throw new ServerProblemException(title);
        }
        _ = response.EnsureSuccessStatusCode();
    }

    private record ProblemResponse(string? Title);
}
