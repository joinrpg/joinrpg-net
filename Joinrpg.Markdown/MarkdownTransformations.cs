using System.Linq;
using JoinRpg.DataModel;

namespace Joinrpg.Markdown
{
    /// <summary>
    /// Some utility functions that allows to transform markdown string to other markdown strings
    /// </summary>
    public static class MarkdownTransformations
    {
        /// <summary>
        /// Return X first words from markdown (keeping formating).
        /// TODO: That method could be improved and unit tested
        /// </summary>
        public static MarkdownString TakeWords(this MarkdownString markdownString, int words)
        {
            if (markdownString?.Contents == null)
            {
                return null;
            }
            var w = words;
            var idx = markdownString.Contents
                .TakeWhile(c => (w -= char.IsWhiteSpace(c) ? 1 : 0) > 0 && c != '\n').Count();
            var mdContents = markdownString.Contents.Substring(0, idx);
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
            MarkdownString oldString)
        {
            //TODO: look for diff algorithms
            return newString;
        }
    }
}
