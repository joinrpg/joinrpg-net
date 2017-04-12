using System;
using System.Text.RegularExpressions;
using System.Web;
using JetBrains.Annotations;
using Joinrpg.Markdown;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Web.Models
{
  /// <summary>
  /// A view model for a single item from search result
  /// carrying, in additon, the target of the made search.
  /// </summary>
  public class TargetedSearchResultViewModel
  { 
    public string SearchTarget { get; private set; }
    public ISearchResult SearchResult { get; private set; }

    public IHtmlString GetFormattedDescription(int maxLengthToShow)
    {
      string descriptionToShow = TruncateString(
        SearchResult.Description.ToPlainText(), SearchTarget, maxLengthToShow);

      descriptionToShow = Regex.Replace(
          descriptionToShow,
          SearchTarget,
          match => "<b><u>" + match.Value + "</u></b>", RegexOptions.IgnoreCase);

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

      int startOfSearchedFragment = stringToTruncate.IndexOf(targetText, stringComparison);

      //show the beginning of the string if target fragment is not found
      startOfSearchedFragment = startOfSearchedFragment < 0 ? 0 : startOfSearchedFragment;

      //Try to put the beginning of the searched fragment in the middle of the substring
      int startOfSubtringToShow = startOfSearchedFragment > (maxLength / 2)
        ? startOfSearchedFragment - (maxLength / 2)
        : 0;

      //Move substring to the left, if needed
      startOfSubtringToShow = (startOfSubtringToShow + maxLength) > stringToTruncate.Length
        ? stringToTruncate.Length - maxLength
        : startOfSubtringToShow;

      string truncatedString = stringToTruncate.Substring(startOfSubtringToShow, maxLength);

      if (startOfSubtringToShow > 0)
      {
        truncatedString = "..." + truncatedString;
      }
      if (startOfSubtringToShow + maxLength < stringToTruncate.Length)
      {
        truncatedString = truncatedString + "...";
      }
      return truncatedString;
    }

    public TargetedSearchResultViewModel(
      [NotNull] ISearchResult searchResult,
      [NotNull] string searchTarget)
    {
      if (searchResult == null)
      {
        throw new ArgumentNullException(nameof(searchResult));
      }
      if (searchTarget == null)
      {
        throw new ArgumentNullException(nameof(searchTarget));
      }
      SearchResult = searchResult;
      SearchTarget = searchTarget;
    }
  }
}
