using System;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using JoinRpg.Helpers.Web;
using JoinRpg.Markdown;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Search;
using Microsoft.AspNetCore.Html;

namespace JoinRpg.Web.Models
{
    /// <summary>
    /// A view model for a single item from search result
    /// carrying, in additon, the target of the made search.
    /// </summary>
    public class TargetedSearchResultViewModel
    {
        private string SearchTarget { get; }
        public ISearchResult SearchResult { get; }
        public GameObjectLinkType LinkType => SearchResult.LinkType.AsViewModel();
        public Uri Url { get; }

        public JoinHtmlString GetFormattedDescription(int maxLengthToShow)
        {
            var descriptionToShow = TruncateString(
                SearchResult.Description.ToPlainText().ToHtmlString(),
                SearchTarget,
                maxLengthToShow);

            descriptionToShow = Regex.Replace(
                descriptionToShow,
                Regex.Escape(SearchTarget),
                match => "<b><u>" + match.Value + "</u></b>",
                RegexOptions.IgnoreCase);

            return new HtmlString(descriptionToShow);
        }

        private static string TruncateString(
            [NotNull] string stringToTruncate,
            [NotNull] string targetText,
            int maxLength,
            StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            if (targetText.Length > maxLength)
            {
                targetText = targetText.Substring(0, maxLength);
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

        public TargetedSearchResultViewModel([NotNull] ISearchResult searchResult,
            [NotNull] string searchTarget,
            IUriService uriService)
        {
            SearchResult = searchResult ?? throw new ArgumentNullException(nameof(searchResult));
            SearchTarget = searchTarget ?? throw new ArgumentNullException(nameof(searchTarget));
            Url = uriService.GetUri(searchResult);
        }
    }
}
