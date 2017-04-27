using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain
{
  public static class TagExtensions
  {
    public static IEnumerable<string> ExtractTagNames([NotNull] this string title)
    {
      if (title == null) throw new ArgumentNullException(nameof(title));
      return ExtractTagNamesImpl(title).Distinct();
    }

    private static IEnumerable<string> ExtractTagNamesImpl(string title)
    {
      for (var i = 0; i < title.Length; i++)
      {
        if (title[i] != '#') continue;

        var tagName = title.Skip(i + 1).TakeWhile(c => char.IsLetterOrDigit(c) || c == '_').AsString().Trim();
        if (tagName != "")
        {
          yield return tagName.ToLowerInvariant();
        }
      }
    }

    public static string RemoveTagNames([NotNull] this string title)
    {
      if (title == null) throw new ArgumentNullException(nameof(title));

      var extractTagNames = title.ExtractTagNames().ToList();
      return title
        .RemoveFromString(extractTagNames.Select(tag => "#" + tag + ","), StringComparison.InvariantCultureIgnoreCase)
        .RemoveFromString(extractTagNames.Select(tag => "#" + tag + ";"), StringComparison.InvariantCultureIgnoreCase)
        .RemoveFromString(extractTagNames.Select(tag => "#" + tag), StringComparison.InvariantCultureIgnoreCase)
        .Trim();
    }

    public static string GetTagString(this IEnumerable<ProjectItemTag> tags)
    {
      return tags.Select(tag => "#" + tag.TagName).JoinStrings(" ");
    }
  }
}
