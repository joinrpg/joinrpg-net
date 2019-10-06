using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace PscbApi.Models
{
    /// <summary>
    /// Query for payment status
    /// </summary>
    public class PaymentInfoQuery
    {
        /// <summary>
        /// Order Id
        /// </summary>
        [Required]
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        /// <summary>
        /// Merchant Id (issued by the bank)
        /// </summary>
        [Required]
        [JsonProperty("marketPlace")]
        public string MerchantId { get; set; }

        /// <summary>
        /// If true, additional card data will be returned
        /// </summary>
        [JsonProperty("requestCardData")]
        public bool GetCardData { get; set; }

        /// <summary>
        /// If true, fiscal documents associated with payment will be returned
        /// </summary>
        [JsonProperty("requestFiscalData")]
        public bool GetFiscalData { get; set; }
    }
}
