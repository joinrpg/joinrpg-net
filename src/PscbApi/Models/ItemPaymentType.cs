using Newtonsoft.Json;
// ReSharper disable IdentifierTypo

namespace PscbApi.Models
{
    /// <summary>
    /// Payment types
    /// </summary>
    [JsonConverter(typeof(IdentifiableEnumConverter))]
    public enum ItemPaymentType
    {
        /// <summary>
        /// Complete payment before transfer of an item to the buyer
        /// </summary>
        [Identifier("full_prepayment")]
        FullPrepayment,

        /// <summary>
        /// Partial payment before transfer of an item to the buyer
        /// </summary>
        [Identifier("prepayment")]
        Prepayment,

        /// <summary>
        /// Advance payment
        /// </summary>
        [Identifier("advance")]
        Advance,

        /// <summary>
        /// Complete payment (including advance) at the moment of transfer to the buyer
        /// </summary>
        [Identifier("full_payment")]
        FullPayment,

        /// <summary>
        /// Partial payment and credit
        /// </summary>
        [Identifier("partial_payment")]
        PartialPayment,

        /// <summary>
        /// Item transferred on credit
        /// </summary>
        [Identifier("credit")]
        Credit,

        /// <summary>
        /// Credit payment
        /// </summary>
        [Identifier("credit_payment")]
        CreditPayment,
    }
}
