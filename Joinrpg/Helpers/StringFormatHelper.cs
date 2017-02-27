using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using JetBrains.Annotations;

namespace JoinRpg.Web.Helpers
{
  public static class StringFormatHelper
  {
    /// <summary>
    /// Returns a substring with first occurrence of <param name="targetText"></param> in the middle
    /// and not longer than the given maxLength
    /// </summary>
    public static string TruncateString(
      [NotNull] string stringToTruncate,
      [NotNull] string targetText,
      int maxLength,
      StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
    {
      if (targetText.Length > maxLength)
      {
        targetText = targetText.Substring(0, maxLength);
      }

      if (stringToTruncate.Length > maxLength)
      {
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

        stringToTruncate = "..." + stringToTruncate.Substring(startOfSubtringToShow, maxLength);

        if (startOfSubtringToShow > 0)
        {
          stringToTruncate = "..." + stringToTruncate;
        }
        if (startOfSubtringToShow + maxLength < stringToTruncate.Length)
        {
          stringToTruncate = stringToTruncate + "...";
        }
      }
      return stringToTruncate;
    }
  }
}