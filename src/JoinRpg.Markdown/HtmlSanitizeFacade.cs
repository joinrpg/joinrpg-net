using JetBrains.Annotations;
using JoinRpg.Helpers.Web;

namespace JoinRpg.Markdown
{
    /// <summary>
    /// Facade for HTML sanitization
    /// </summary>
    public static class HtmlSanitizeFacade
    {
        /// <summary>
        /// Remove all Html from string
        /// </summary>
        /// <returns>We are returning <see cref="JoinHtmlString"/> to signal "no need to sanitize this again"</returns>
        [NotNull]
        public static JoinHtmlString RemoveHtml([NotNull]
            this UnSafeHtml unsafeHtml)
        {
            if (unsafeHtml == null)
            {
                throw new ArgumentNullException(nameof(unsafeHtml));
            }

            return HtmlSanitizers.RemoveAll.Sanitize(unsafeHtml.UnValidatedValue).MarkAsHtmlString();
        }

        /// <summary>
        /// Sanitize all Html, leaving safe subset
        /// </summary>
        /// <returns>We are returning <see cref="JoinHtmlString"/> to signal "no need to sanitize this again"</returns>
        [NotNull]
        public static JoinHtmlString SanitizeHtml([NotNull]
            this UnSafeHtml unsafeHtml)
        {
            if (unsafeHtml == null)
            {
                throw new ArgumentNullException(nameof(unsafeHtml));
            }

            return HtmlSanitizers.Simple.Sanitize(unsafeHtml.UnValidatedValue).MarkAsHtmlString();
        }

        /// <summary>
        /// Sanitize all Html, leaving safe subset
        /// </summary>
        /// <returns>We are returning <see cref="JoinHtmlString"/> to signal "no need to sanitize this again"</returns>
        [NotNull]
        public static JoinHtmlString SanitizeHtml([NotNull]
            this string str)
        {
            var unsafeHtml = (UnSafeHtml?)str;
            if (unsafeHtml == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            return unsafeHtml.SanitizeHtml();
        }
    }
}
