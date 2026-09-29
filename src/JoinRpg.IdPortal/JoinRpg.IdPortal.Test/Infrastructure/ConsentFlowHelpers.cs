using System.Net;
using HtmlAgilityPack;
using JoinRpg.IdPortal.OAuthServer;

namespace JoinRpg.IdPortal.Test.Infrastructure;

/// <summary>
/// Проходит авторизацию целиком, включая экран согласия. Согласие выдаётся только POST-ом
/// формы с antiforgery-токеном (см. <see cref="OAuthConsent"/>), поэтому тесты берут форму
/// из отрисованной страницы и отправляют её так, как это сделал бы браузер.
/// </summary>
public static class ConsentFlowHelpers
{
    public const string ConsentFormId = "oauth-consent";

    /// <summary>
    /// Идёт на <c>connect/authorize</c> и, если сервер увёл на экран согласия, отвечает на него.
    /// Возвращает ответ <c>connect/authorize</c> с итоговым решением — обычно редирект на клиента.
    /// Клиент должен быть создан с <c>AllowAutoRedirect = false</c>.
    /// </summary>
    public static async Task<HttpResponseMessage> AuthorizeWithConsentAsync(
        this HttpClient client, string authorizeUrl, bool grant = true, params int[] projectIds)
    {
        var authorize = await client.GetAsync(authorizeUrl);

        // Согласие уже выдано (или запрос отлетел) — отвечать не на что.
        if (authorize.StatusCode != HttpStatusCode.Found
            || authorize.Headers.Location?.ToString().Contains("/oauth/consent") != true)
        {
            return authorize;
        }

        var page = await client.GetAsync(authorize.Headers.Location.ToString());
        page.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fields = GetConsentFormFields(await page.AsHtmlDocument(), grant, projectIds);
        var submitted = await client.PostAsync(page.RequestMessage!.RequestUri, new FormUrlEncodedContent(fields!));

        // Страница возвращает пользователя обратно на connect/authorize — там и решается исход.
        submitted.StatusCode.ShouldBe(HttpStatusCode.Found);
        return await client.GetAsync(submitted.Headers.Location!.ToString());
    }

    /// <summary>
    /// Поля формы согласия: скрытые поля страницы (в том числе antiforgery-токен и имя формы),
    /// нажатая кнопка и отмеченные чекбоксы проектов.
    /// </summary>
    public static List<KeyValuePair<string, string?>> GetConsentFormFields(
        this HtmlDocument doc, bool grant, params int[] projectIds)
    {
        var form = doc.DocumentNode.SelectSingleNode($"//form[@id='{ConsentFormId}']");
        form.ShouldNotBeNull();

        var fields = (form.SelectNodes(".//input[@type='hidden']") ?? Enumerable.Empty<HtmlNode>())
            .Select(input => new KeyValuePair<string, string?>(
                input.GetAttributeValue("name", ""),
                WebUtility.HtmlDecode(input.GetAttributeValue("value", ""))))
            .Where(field => field.Key.Length > 0)
            .ToList();

        fields.Add(new(OAuthConsent.DecisionField, grant ? OAuthConsent.Granted : OAuthConsent.Denied));
        fields.AddRange(projectIds.Select(id =>
            new KeyValuePair<string, string?>(OAuthConsent.ProjectsParameter, id.ToString())));

        return fields;
    }
}
