namespace PscbApi.Models
{
    /// <summary>
    /// Query params for payment using API
    /// </summary>
    public class PaymentQueryParams
    {
        public string marketPlace { get; set; }

        public string message { get; set; }

        public string signature { get; set; }
    }
}
