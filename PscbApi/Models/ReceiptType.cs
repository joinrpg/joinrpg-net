using Newtonsoft.Json;

namespace PscbApi.Models {

    /// <summary>
    /// Receipt type
    /// </summary>
    [JsonConverter(typeof(IdentifiableEnumConverter))]
    public enum ReceiptType
    {
        /// <summary>
        /// Income receipt
        /// </summary>
        [Identifier("income")]
        Income,

        /// <summary>
        /// Refund receipt
        /// </summary>
        [Identifier("refund")]
        Refund,
    }
}
