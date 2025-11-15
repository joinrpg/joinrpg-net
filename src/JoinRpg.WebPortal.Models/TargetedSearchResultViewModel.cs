using System.Text.RegularExpressions;
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
        var descriptionToShow = TruncateString(
            SearchResult.Description.ToPlainTextAndEscapeHtml().Value,
            SearchTarget,
            maxLengthToShow);

        descriptionToShow = Regex.Replace(
            descriptionToShow,
            Regex.Escape(SearchTarget),
            match => "<b><u>" + match.Value + "</u></b>",
            RegexOptions.IgnoreCase);

        return new MarkupString(descriptionToShow);
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
