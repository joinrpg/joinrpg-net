using System.Net;
using HtmlAgilityPack;

namespace JoinRpg.IntegrationTests.TestInfrastructure
{
    public static class HtmlDocumentHelpers
    {
        public static string? GetTitle(this HtmlDocument doc)
        {
            if (doc.DocumentNode.SelectSingleNode("//head/title") is HtmlNode titleNode)
            {
                return WebUtility.HtmlDecode(titleNode.InnerText);
            }
            else
            {
                return null;
            }
        }

        public static async Task<HtmlDocument> AsHtmlDocument(this HttpResponseMessage response)
        {
            var responseText = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(responseText);
            return doc;
        }
    }
}
