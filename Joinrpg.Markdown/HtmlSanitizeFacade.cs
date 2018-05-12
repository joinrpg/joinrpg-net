using System;
using System.Web;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace Joinrpg.Markdown
{
    /// <summary>
    /// Facade for HTML sanitization
    /// </summary>
    public static class HtmlSanitizeFacade
    {
        /// <summary>
        /// Remove all Html from string
        /// </summary>
        /// <returns>We are returning <see cref="IHtmlString"/> to signal "no need to sanitize this again"</returns>
        [NotNull]
        public static IHtmlString RemoveHtml([NotNull]
            this UnSafeHtml unsafeHtml)
        {
            if (unsafeHtml == null) throw new ArgumentNullException(nameof(unsafeHtml));
            return new HtmlString(HtmlSanitizers.RemoveAll.Sanitize(unsafeHtml.UnValidatedValue));
        }

        /// <summary>
        /// Sanitize all Html, leaving safe subset
        /// </summary>
        /// <returns>We are returning <see cref="IHtmlString"/> to signal "no need to sanitize this again"</returns>
        [NotNull]
        public static IHtmlString SanitizeHtml([NotNull]
            this UnSafeHtml unsafeHtml)
        {
            if (unsafeHtml == null) throw new ArgumentNullException(nameof(unsafeHtml));
            return new HtmlString(HtmlSanitizers.Simple.Sanitize(unsafeHtml.UnValidatedValue));
        }

        /// <summary>
        /// Sanitize all Html, leaving safe subset
        /// </summary>
        /// <returns>We are returning <see cref="IHtmlString"/> to signal "no need to sanitize this again"</returns>
        [NotNull]
        public static IHtmlString SanitizeHtml([NotNull]
            this string str)
        {
            var unsafeHtml = (UnSafeHtml) str;
            if (unsafeHtml == null) throw new ArgumentNullException(nameof(str));
            return unsafeHtml.SanitizeHtml();
        }
    }
}
