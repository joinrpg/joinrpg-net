using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Joinrpg.AspNetCore.Helpers
{
    /// <summary>
    /// Helper to accomodate to IHtmlContent semantic changes in core (.ToString() no longer returns rendered content)
    /// </summary>
    public static class HtmlContentJoinHelper
    {
        /// <summary>
        /// Join IHtmlContents them with separator
        /// </summary>
        public static IHtmlContent JoinList(this IHtmlHelper self, string rawSeparator, IEnumerable<IHtmlContent> contents)
        {
            var builder = new HtmlContentBuilder();
            var writeSep = false;
            foreach (var c in contents)
            {
                if (writeSep)
                {
                    _ = builder.AppendHtml(rawSeparator);
                }
                writeSep = true;
                _ = builder.AppendHtml(c);
            }
            return builder;
        }

        /// <summary>
        /// Join IHtmlContents them with separator
        /// </summary>
        public static IHtmlContent JoinList(this IHtmlHelper self, string rawSeparator, params IHtmlContent[] contents)
            => self.JoinList(rawSeparator, contents.AsEnumerable());

        /// <summary>
        /// Call Html.DisplayFor(..) for every element in list and then joins them with separator
        /// </summary>
        public static IHtmlContent JoinDisplayFor<TModel, TListItem, TResult>(this IHtmlHelper<TModel> self, string rawSeparator, IEnumerable<TListItem> list, Func<TListItem, TResult> selector)
        {
            IEnumerable<IHtmlContent> ToDisplayFor()
            {
                foreach (var item in list)
                {
                    var selectedItem = selector(item);
                    yield return self.DisplayFor(model => item);
                }
            }

            return self.JoinList(rawSeparator, ToDisplayFor());
        }

        /// <summary>
        /// Call Html.DisplayFor(..) for every element in list and then joins them with separator
        /// </summary>
        public static IHtmlContent JoinDisplayFor<TModel, TListItem>(this IHtmlHelper<TModel> self, string rawSeparator, IEnumerable<TListItem> list) => self.JoinDisplayFor(rawSeparator, list, item => item);
    }
}
