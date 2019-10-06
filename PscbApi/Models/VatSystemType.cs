// ReSharper disable IdentifierTypo
using Newtonsoft.Json;

namespace PscbApi.Models {

    /// <summary>
    /// Allowed VATs
    /// </summary>
    [JsonConverter(typeof(IdentifiableEnumConverter))]
    public enum VatSystemType
    {
        /// <summary>
        /// VAT is not applicable
        /// </summary>
        [Identifier("none")]
        None,

        /// <summary>
        /// VAT is 0
        /// </summary>
        [Identifier("vat0")]
        Vat0,

        /// <summary>
        /// VAT is 10%
        /// </summary>
        [Identifier("vat10")]
        Vat10,

        /// <summary>
        /// VAT is 20%
        /// </summary>
        [Identifier("vat20")]
        Vat20,

        /// <summary>
        /// VAT is calculated as 10/110
        /// </summary>
        [Identifier("vat110")]
        Vat110,

        /// <summary>
        /// VAT is calculated as 20/120
        /// </summary>
        [Identifier("vat120")]
        Vat120,
    }
}
