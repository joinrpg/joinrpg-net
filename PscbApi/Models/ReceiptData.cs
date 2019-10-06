using System;
using Newtonsoft.Json;

namespace PscbApi.Models {

    /// <summary>
    /// Information on fiscal document, associated with payment, returned in <see cref="PaymentInfo"/>
    /// </summary>
    public class ReceiptData
    {
        /// <summary>
        /// Receipt identifier
        /// </summary>
        [JsonProperty("receiptId")]
        public string Id { get; set; }

        /// <summary>
        /// Receipt type
        /// </summary>
        [JsonProperty("type")]
        public ReceiptType Type { get; set; }

        /// <summary>
        /// Fiscal number of the document
        /// </summary>
        [JsonProperty("docNumber")]
        public string DocumentNumber { get; set; }

        /// <summary>
        /// Date and time of fiscal document
        /// </summary>
        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }

        /// <summary>
        /// Money amount
        /// </summary>
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Online register number
        /// </summary>
        [JsonProperty("kkt")]
        public string RegisterNumber { get; set; }

        /// <summary>
        /// Fiscal number
        /// </summary>
        [JsonProperty("fiscalNumber")]
        public string FiscalNumber { get; set; }

        /// <summary>
        /// Shift number
        /// </summary>
        [JsonProperty("shiftNumber")]
        public string ShiftNumber { get; set; }

        /// <summary>
        /// Receipt number within shift
        /// </summary>
        [JsonProperty("receiptNumber")]
        public string ReceiptNumber { get; set; }
    }
}
