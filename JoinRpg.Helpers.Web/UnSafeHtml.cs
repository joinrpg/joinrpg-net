using JetBrains.Annotations;

namespace JoinRpg.Helpers.Web
{
    /// <summary>
    /// Marker class (requires HTML sanitize)
    /// </summary>
    public class UnSafeHtml
    {
        /// <summary>
        /// Value that requires validation
        /// </summary>
        [NotNull]
        public string UnValidatedValue { get; }

        private UnSafeHtml([NotNull]
            string value) => UnValidatedValue = value;

        /// <summary>
        /// Conversion from string
        /// </summary>
        [CanBeNull]
        public static implicit operator UnSafeHtml([CanBeNull]
            string s)
        {
            return s == null ? null : new UnSafeHtml(s);
        }

        /// <summary>
        /// Do not show UnvalidatedValue to prevent string.Format extracting it to
        /// not-markered string
        /// </summary>
        public override string ToString() => "UnSafeHtml";
    }
}
