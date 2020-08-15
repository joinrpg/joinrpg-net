namespace PscbApi.Models
{
    /// <summary>
    /// Descriptor of a payment request to be sent to bank api endpoint using POST method
    /// </summary>
    public class PaymentRequestDescriptor
    {
        /// <summary>
        /// true if debug mode enabled
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Url to send request to
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Query parameters
        /// </summary>
        public PaymentQueryParams QueryParams { get; set; }
    }
}
