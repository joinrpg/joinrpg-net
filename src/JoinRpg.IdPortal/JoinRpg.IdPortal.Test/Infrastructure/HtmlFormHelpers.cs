using System.Net;
using HtmlAgilityPack;

namespace JoinRpg.IdPortal.Test.Infrastructure;

public static class HtmlFormHelpers
{
    /// <summary>
    /// Extracts all input fields from the form (including hidden Blazor SSR handler fields and antiforgery token).
    /// Optionally filters by form name attribute or first found form.
    /// </summary>
    public static Dictionary<string, string?> GetFormFields(this HtmlDocument doc, string? formSelector = null)
    {
        var form = formSelector is not null
            ? doc.DocumentNode.SelectSingleNode($"//form[contains(@action,'{formSelector}') or @data-form-name='{formSelector}']")
            : doc.DocumentNode.SelectSingleNode("//form");

        if (form is null)
        {
            return [];
        }

        var result = new Dictionary<string, string?>();
        foreach (var input in form.SelectNodes(".//input") ?? Enumerable.Empty<HtmlNode>())
        {
            var name = input.GetAttributeValue("name", string.Empty);
            var value = input.GetAttributeValue("value", string.Empty);
            if (!string.IsNullOrEmpty(name))
            {
                result[name] = value;
            }
        }
        return result;
    }

    public static string? GetAntiForgeryToken(this HtmlDocument doc)
    {
        var node = doc.DocumentNode.SelectSingleNode("//input[@name='__RequestVerificationToken']");
        return node?.GetAttributeValue("value", string.Empty);
    }

    public static async Task<HtmlDocument> AsHtmlDocument(this HttpResponseMessage response)
    {
        var responseText = await response.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(responseText);
        return doc;
    }

    public static string? GetTitle(this HtmlDocument doc)
    {
        var titleNode = doc.DocumentNode.SelectSingleNode("//head/title");
        return titleNode is not null ? WebUtility.HtmlDecode(titleNode.InnerText) : null;
    }
}
