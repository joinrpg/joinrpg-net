namespace JoinRpg.Web.Models.Money
{
    /// <summary>
    /// Model for money representation
    /// </summary>
    public class Money
    {
        /// <summary>
        /// Money value
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Html Id
        /// </summary>
        public string HtmlId { get; set; }

        /// <summary>
        /// Class name part with currency code
        /// </summary>
        public string CurrencyCode { get; set; } = "RUR";

        /// <summary>
        /// Returns Id attribute
        /// </summary>
        public string IdAttribute => string.IsNullOrWhiteSpace(HtmlId) ? "" : $"id={HtmlId.Trim()}";

        /// <summary>
        /// Creates new empty Money model
        /// </summary>
        public Money() { }

        /// <summary>
        /// Creates new Money model with specified value
        /// </summary>
        public Money(int value) : this(value, null, null) { }

        /// <summary>
        /// Creates new Money model with specified value and html Id
        /// </summary>
        public Money(int value, string htmlId) : this(value, htmlId, null) { }

        /// <summary>
        /// Creates new Money model with specified value, html Id and currency css class code
        /// </summary>
        public Money(int value, string htmlId, string currencyCode)
        {
            Value = value;
            HtmlId = htmlId ?? HtmlId;
            CurrencyCode = currencyCode ?? CurrencyCode;
        }
    }
}
