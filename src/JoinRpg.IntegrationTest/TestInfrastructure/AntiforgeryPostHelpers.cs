using System.Net;
using System.Net.Http.Headers;
using HtmlAgilityPack;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// POST-запросы из интеграционных тестов: получение antiforgery-токена и отправка формы или
/// ajax-запроса с ним.
/// </summary>
/// <remarks>
/// Antiforgery в портале включён глобально (<c>AutoValidateAntiforgeryTokenAttribute</c> в
/// <c>Startup</c>), поэтому без токена не проходит ни один POST — в том числе те ручки, у которых
/// нет своего <c>[ValidateAntiForgeryToken]</c>. Токен привязан к cookie, поэтому его достаточно
/// получить один раз на клиент и переиспользовать во всех POST этого клиента.
///
/// Браузер отправляет токен двумя разными способами, и оба надо уметь воспроизвести:
/// <list type="bullet">
/// <item><description>
/// обычная форма — скрытым полем <c>__RequestVerificationToken</c>
/// (<see cref="PostFormAsync"/>);
/// </description></item>
/// <item><description>
/// ajax со страницы — заголовком <c>X-CSRF-TOKEN</c> (имя задано в
/// <c>AuthenticationConfigurator</c>): тело либо пустое, а параметры лежат в query
/// (<see cref="PostWithTokenHeaderAsync"/>), либо JSON — так ходят Blazor-острова
/// (<see cref="PostJsonWithTokenHeaderAsync"/>).
/// </description></item>
/// </list>
/// </remarks>
public static class AntiforgeryPostHelpers
{
    /// <summary>Имя скрытого поля формы с токеном.</summary>
    public const string FormFieldName = "__RequestVerificationToken";

    /// <summary>Заголовок, которым токен передаёт клиентский скрипт.</summary>
    public const string HeaderName = "X-CSRF-TOKEN";

    /// <summary>
    /// Открывает страницу и достаёт со неё antiforgery-токен.
    /// </summary>
    /// <param name="client">Клиент, от имени которого потом пойдёт POST: токен привязан к его cookie.</param>
    /// <param name="pageUrl">Любая страница этого клиента, на которой есть форма с токеном.</param>
    public static async Task<string> GetAntiforgeryTokenAsync(this HttpClient client, string pageUrl)
    {
        var response = await client.GetAsync(pageUrl);
        response.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            $"Страница {pageUrl}, с которой берётся antiforgery-токен, не открылась");

        return await response.GetAntiforgeryTokenAsync(pageUrl);
    }

    /// <summary>
    /// Достаёт antiforgery-токен из уже полученного ответа — когда страница и так нужна тесту.
    /// </summary>
    public static async Task<string> GetAntiforgeryTokenAsync(
        this HttpResponseMessage response,
        string pageUrl)
    {
        HtmlDocument document = await response.AsHtmlDocument();
        return document.DocumentNode
            .SelectSingleNode($"//input[@name='{FormFieldName}']")?
            .GetAttributeValue("value", "")
            ?? throw new InvalidOperationException($"На странице {pageUrl} нет antiforgery-токена");
    }

    /// <summary>
    /// Отправляет форму так, как это делает браузер: поля плюс скрытое поле с токеном.
    /// </summary>
    /// <remarks>Код ответа не проверяется: его смысл у каждой ручки свой.</remarks>
    public static Task<HttpResponseMessage> PostFormAsync(
        this HttpClient client,
        string postUrl,
        string antiforgeryToken,
        params (string Name, string Value)[] fields)
    {
        var content = fields
            .Select(field => new KeyValuePair<string?, string?>(field.Name, field.Value))
            .Append(new KeyValuePair<string?, string?>(FormFieldName, antiforgeryToken));

        return client.PostAsync(postUrl, new FormUrlEncodedContent(content));
    }

    /// <summary>
    /// Отправляет POST без тела, передавая токен заголовком — так ходят скрипты страниц.
    /// </summary>
    /// <param name="antiforgeryToken">
    /// Токен; <c>null</c> — чтобы проверить, что ручка без токена запрос не принимает.
    /// </param>
    public static Task<HttpResponseMessage> PostWithTokenHeaderAsync(
        this HttpClient client,
        string postUrl,
        string? antiforgeryToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, postUrl)
        {
            // Пустое тело с явным типом: без Content-Length ручка получит запрос без формы, и
            // antiforgery не сможет откатиться на поле формы — токен должен браться из заголовка.
            Content = new ByteArrayContent([]),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        if (antiforgeryToken is not null)
        {
            request.Headers.Add(HeaderName, antiforgeryToken);
        }

        return client.SendAsync(request);
    }

    /// <summary>
    /// Отправляет POST с JSON-телом, передавая токен заголовком — так ходят Blazor-острова.
    /// </summary>
    /// <param name="antiforgeryToken">
    /// Токен; <c>null</c> — чтобы проверить, что ручка без токена запрос не принимает.
    /// </param>
    public static Task<HttpResponseMessage> PostJsonWithTokenHeaderAsync<TBody>(
        this HttpClient client,
        string postUrl,
        TBody body,
        string? antiforgeryToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, postUrl)
        {
            Content = System.Net.Http.Json.JsonContent.Create(body),
        };

        if (antiforgeryToken is not null)
        {
            request.Headers.Add(HeaderName, antiforgeryToken);
        }

        return client.SendAsync(request);
    }

    /// <summary>
    /// Достаёт из вернувшейся формы текст ошибок валидации — иначе падение теста выглядит как
    /// «ожидался 302, получен 200» без всякого объяснения.
    /// </summary>
    public static async Task<string> DescribeValidationErrorsAsync(this HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return $"ответ {(int)response.StatusCode}";
        }

        HtmlDocument document = await response.AsHtmlDocument();
        var errors = document.DocumentNode
            .SelectNodes("//*[contains(@class, 'validation-summary-errors')]|//*[contains(@class, 'field-validation-error')]")
            ?.Select(node => WebUtility.HtmlDecode(node.InnerText).Trim())
            .Where(text => text.Length > 0)
            .ToList() ?? [];

        return errors.Count > 0 ? string.Join("; ", errors) : "форма вернулась без явных ошибок валидации";
    }
}
