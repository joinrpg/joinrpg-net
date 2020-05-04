using Newtonsoft.Json;

namespace PscbApi.Models
{

    /// <summary>
    /// Status of payment information query
    /// </summary>
    [JsonConverter(typeof(IdentifiableEnumConverter))]
    public enum PaymentInfoQueryStatus
    {
        /// <summary>
        /// Payment found
        /// </summary>
        [Identifier("STATUS_SUCCESS")]
        Success,

        /// <summary>
        /// Error occured
        /// </summary>
        [Identifier("STATUS_FAIL")]
        Fail,

        /// <summary>
        /// Error occured
        /// </summary>
        [Identifier("STATUS_FAILURE")]
        Failure,
    }
}
