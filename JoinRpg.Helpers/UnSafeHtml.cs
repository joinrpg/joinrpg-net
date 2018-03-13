using JetBrains.Annotations;

namespace JoinRpg.Helpers
{
    public class UnSafeHtml
    {
        [NotNull]
        public string UnValidatedValue { get; }

        private UnSafeHtml([NotNull]
            string value)
        {
            UnValidatedValue = value;
        }

        [CanBeNull]
        public static implicit operator UnSafeHtml([CanBeNull]
            string s)
        {
            return s == null ? null : new UnSafeHtml(s);
        }

        public override string ToString() => $"UnSafeHtml({UnValidatedValue})";
    }
}
