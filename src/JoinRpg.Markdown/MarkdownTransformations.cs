using System.Diagnostics.CodeAnalysis;
using JoinRpg.DataModel;

namespace JoinRpg.Markdown;

/// <summary>
/// Some utility functions that allows to transform markdown string to other markdown strings
/// </summary>
public static class MarkdownTransformations
{
    /// <summary>
    /// Return X first words from markdown (keeping formating).
    /// TODO: That method could be improved and unit tested
    /// </summary>
    [return: NotNullIfNotNull(nameof(markdownString))]
    public static MarkdownString? TakeWords(this MarkdownString? markdownString, int words)
    {
        if (markdownString?.Contents == null)
        {
            return null;
        }
        var w = words;
        var str = markdownString.Contents;
        var idx = 0;
        while (w > 0 && idx < str.Length)
        {
            if (str[idx] == '\n')
            {
                break;
            }
            if (char.IsWhiteSpace(str[idx]))
            {
                w--;
            }
            idx++;
        }
        if (str.Length == idx)
        {
            return markdownString;
        }
        var mdContents = markdownString.Contents[..idx] + "...";

        return new MarkdownString(mdContents);
    }

    /// <summary>
    /// Return diffs between 2 strings.
    /// TODO: not implemented yet
    /// </summary>
    public static MarkdownString HighlightDiffPlaceholder(string newString, string oldString)
    {
        //TODO: look for diff algorithms
        return new MarkdownString(newString);
    }

    /// <summary>
    /// Return diffs between 2 strings.
    /// TODO: not implemented yet
    /// </summary>
    public static MarkdownString HighlightDiffPlaceholder(MarkdownString newString,
        MarkdownString oldString) =>
        //TODO: look for diff algorithms
        newString;
}
