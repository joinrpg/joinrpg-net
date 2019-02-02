using System;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Html;

namespace JoinRpg.Helpers.Web
{
    /// <summary>
    /// Convience functions for IHtmlString
    /// </summary>
    public static class HtmlStringExtensions
    {
        /// <summary>
        /// Default value for IHtmlString
        /// </summary>
        [NotNull]
        public static JoinHtmlString WithDefaultStringValue([NotNull] this JoinHtmlString toHtmlString, [NotNull] string defaultValue)
        {
            if (toHtmlString == null) throw new ArgumentNullException(nameof(toHtmlString));
            if (defaultValue == null) throw new ArgumentNullException(nameof(defaultValue));
            return new HtmlString(toHtmlString.ToHtmlString().WithDefaultStringValue(defaultValue));
        }
    }
}
