using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Разбор ответа webapi в HTTP-клиентах островов: отказ сервера, который можно объяснить пользователю,
/// превращается в <see cref="ServerProblemException"/>.
/// </summary>
public static class HttpResponseProblemExtensions
{
    private const string UnauthorizedMessage = "Вы не вошли на сайт или сессия истекла — обновите страницу";
    private const string ForbiddenMessage = "Недостаточно прав для этого действия";

    /// <summary>
    /// Как <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/>, но если сервер объяснил отказ,
    /// бросает <see cref="ServerProblemException"/> с его текстом — его можно показать пользователю.
    /// Объяснением 4xx считаются ProblemDetails (Title или ошибки валидации), текст или JSON-строка в теле.
    /// На 401 и 403 без объяснения — свой текст. Остальное, в том числе любой 5xx, — <see cref="HttpRequestException"/>.
    /// </summary>
    public static async Task EnsureSuccessOrThrowProblem(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }
        if (await TryReadRefusal(response) is string reason)
        {
            throw new ServerProblemException(reason);
        }
        _ = response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// <see cref="EnsureSuccessOrThrowProblem"/>, затем чтение тела как JSON. Пустое тело — ошибка.
    /// </summary>
    public static async Task<T> ReadFromJsonOrThrowProblem<T>(this HttpResponseMessage response)
    {
        await response.EnsureSuccessOrThrowProblem();
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new Exception("Couldn't get result from server");
    }

    private static async Task<string?> TryReadRefusal(HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;

        // Отказ — только 4xx. 5xx — сбой: остров покажет свой текст и запишет ошибку в лог,
        // а у тела там может оказаться что угодно («Неизвестная ошибка на сервере» в лучшем случае)
        if ((int)response.StatusCode is >= 400 and < 500)
        {
            if (IsMediaType(mediaType, "application/problem+json") && await TryReadProblem(response.Content) is string problem)
            {
                return problem;
            }
            // BadRequest("…") из MVC-контроллера
            if (IsMediaType(mediaType, "text/plain") && NotBlank(await TryRead(response.Content.ReadAsStringAsync)) is string text)
            {
                return text;
            }
            // TypedResults.BadRequest("…") приходит JSON-строкой
            if (IsMediaType(mediaType, "application/json")
                && NotBlank(await TryRead(() => response.Content.ReadFromJsonAsync<string>())) is string value)
            {
                return value;
            }
        }

        // Без тела: cookie-аутентификация на /webapi не редиректит, а отдаёт голый код
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => UnauthorizedMessage,
            HttpStatusCode.Forbidden => ForbiddenMessage,
            _ => null,
        };
    }

    private static async Task<string?> TryReadProblem(HttpContent content)
    {
        var problem = await TryRead(() => content.ReadFromJsonAsync<ProblemResponse>());
        // У ошибок валидации [ApiController] заголовок английский, а смысл — в errors
        var errors = problem?.Errors?.Values.SelectMany(messages => messages).Select(NotBlank).OfType<string>().ToList();
        if (errors is { Count: > 0 })
        {
            return string.Join(" ", errors);
        }
        return NotBlank(problem?.Title);
    }

    /// <summary>
    /// Нечитаемое тело — не объяснение отказа: битый JSON (<see cref="JsonException"/>)
    /// или неизвестная кодировка в Content-Type (<see cref="InvalidOperationException"/>).
    /// </summary>
    private static async Task<T?> TryRead<T>(Func<Task<T>> read)
    {
        try
        {
            return await read();
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException)
        {
            return default;
        }
    }

    private static bool IsMediaType(string? actual, string expected)
        => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static string? NotBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private record ProblemResponse(string? Title, Dictionary<string, string[]>? Errors);
}
