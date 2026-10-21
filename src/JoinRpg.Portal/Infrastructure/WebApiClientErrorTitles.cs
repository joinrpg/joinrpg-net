using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Infrastructure;

/// <summary>
/// Голый <c>BadRequest()</c>/<c>NotFound()</c> у <c>[ApiController]</c> превращается в ProblemDetails с заголовком
/// из <see cref="ApiBehaviorOptions.ClientErrorMapping"/>. Клиенты островов показывают Title пользователю как есть,
/// поэтому заголовки — по-русски.
/// </summary>
internal static class WebApiClientErrorTitles
{
    private static readonly Dictionary<int, string> Titles = new()
    {
        [StatusCodes.Status400BadRequest] = "Некорректный запрос",
        [StatusCodes.Status401Unauthorized] = "Вы не вошли на сайт или сессия истекла — обновите страницу",
        [StatusCodes.Status403Forbidden] = "Недостаточно прав для этого действия",
        [StatusCodes.Status404NotFound] = "Не найдено",
        [StatusCodes.Status405MethodNotAllowed] = "Метод не поддерживается",
        [StatusCodes.Status406NotAcceptable] = "Формат ответа не поддерживается",
        [StatusCodes.Status408RequestTimeout] = "Истекло время ожидания запроса",
        [StatusCodes.Status409Conflict] = "Конфликт изменений — обновите страницу",
        [StatusCodes.Status412PreconditionFailed] = "Данные изменились — обновите страницу",
        [StatusCodes.Status415UnsupportedMediaType] = "Формат запроса не поддерживается",
        [StatusCodes.Status422UnprocessableEntity] = "Запрос не удалось обработать",
        [StatusCodes.Status500InternalServerError] = "Неизвестная ошибка на сервере",
    };

    public static void Configure(ApiBehaviorOptions options)
    {
        foreach (var (status, title) in Titles)
        {
            options.ClientErrorMapping[status] = new ClientErrorData
            {
                Title = title,
                Link = options.ClientErrorMapping.TryGetValue(status, out var existing) ? existing.Link : null,
            };
        }
    }
}
