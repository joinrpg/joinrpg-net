using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using JoinRpg.Markdown;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Search;
using JoinRpg.Web.Games.Projects;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Models;

/// <summary>
/// A view model for a single item from search result
/// carrying, in additon, the target of the made search.
/// </summary>
public class TargetedSearchResultViewModel(SearchResult searchResult,
    string searchTarget,
    IUriService uriService,
    ProjectListItemViewModel? projectViewModel)
{
    private string SearchTarget { get; } = searchTarget ?? throw new ArgumentNullException(nameof(searchTarget));
    public ProjectListItemViewModel? ProjectViewModel { get; } = projectViewModel;
    public SearchResult SearchResult { get; } = searchResult ?? throw new ArgumentNullException(nameof(searchResult));
    public GameObjectLinkType LinkType => SearchResult.LinkType.AsViewModel();
    public Uri Url { get; } = uriService.GetUri(searchResult);

    public JoinHtmlString GetFormattedDescription(int maxLengthToShow)
    {
        // Work on decoded plain text so that search terms with '&', '<' etc. are found correctly.
        // ToPlainTextWithoutHtmlEscape returns Markdig output which may contain HTML entities;
        // HtmlDecode converts them to raw characters before searching.
        var plainText = WebUtility.HtmlDecode(
            ((MarkdownString?)SearchResult.Description).ToPlainTextWithoutHtmlEscape());

        var truncated = TruncateString(plainText, SearchTarget, maxLengthToShow);

        // Sanitization (HTML encoding) happens here, per segment, not up front.
        return BuildHighlightedHtml(truncated, SearchTarget);
    }

    private static MarkupString BuildHighlightedHtml(string plainText, string searchTarget)
    {
        if (string.IsNullOrEmpty(searchTarget))
        {
            return new MarkupString(HtmlEncoder.Default.Encode(plainText));
        }

        var sb = new StringBuilder();
        var sw = new StringWriter(sb);
        var pos = 0;

        while (pos < plainText.Length)
        {
            var matchIndex = plainText.IndexOf(searchTarget, pos, StringComparison.InvariantCultureIgnoreCase);
            if (matchIndex < 0)
            {
                HtmlEncoder.Default.Encode(sw, plainText, pos, plainText.Length - pos);
                break;
            }

            HtmlEncoder.Default.Encode(sw, plainText, pos, matchIndex - pos);
            _ = sb.Append("<b><u>");
            HtmlEncoder.Default.Encode(sw, plainText, matchIndex, searchTarget.Length);
            _ = sb.Append("</u></b>");

            pos = matchIndex + searchTarget.Length;
        }

        return new MarkupString(sb.ToString());
    }

    private static string TruncateString(
        string stringToTruncate,
        string targetText,
        int maxLength,
        StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
    {
        if (targetText.Length > maxLength)
        {
            targetText = targetText[..maxLength];
        }
        if (stringToTruncate.Length <= maxLength)
        {
            return stringToTruncate;
        }

        var startOfSearchedFragment = stringToTruncate.IndexOf(targetText, stringComparison);

        //show the beginning of the string if target fragment is not found
        startOfSearchedFragment = startOfSearchedFragment < 0 ? 0 : startOfSearchedFragment;

        //Try to put the beginning of the searched fragment in the middle of the substring
        var startOfSubtringToShow = startOfSearchedFragment > (maxLength / 2)
            ? startOfSearchedFragment - (maxLength / 2)
            : 0;

        //Move substring to the left, if needed
        startOfSubtringToShow = (startOfSubtringToShow + maxLength) > stringToTruncate.Length
            ? stringToTruncate.Length - maxLength
            : startOfSubtringToShow;

        var truncatedString = stringToTruncate.Substring(startOfSubtringToShow, maxLength);

        if (startOfSubtringToShow > 0)
        {
            truncatedString = "..." + truncatedString;
        }
        if (startOfSubtringToShow + maxLength < stringToTruncate.Length)
        {
            truncatedString += "...";
        }
        return truncatedString;
    }
}
