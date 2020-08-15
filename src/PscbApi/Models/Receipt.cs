// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace PscbApi.Models
{

    /// <summary>
    /// Receipt data
    /// </summary>
    /// <remarks>
    /// See https://docs.pscb.ru/oos/fiscal.html#onlajn-kassa-opisanie-parametrov-cheka for details
    /// </remarks>
    public class Receipt : IValidatableObject
    {
        /// <summary>
        /// Tax system. Required if items with different tax types are in <see cref="Items"/>
        /// </summary>
        [JsonProperty("taxSystem", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public TaxSystemType? TaxSystem { get; set; }

        /// <summary>
        /// Email of a company that owns a Store
        /// </summary>
        [Required]
        [EmailAddress]
        [JsonProperty("companyEmail")]
        public string CompanyEmail { get; set; }

        /// <summary>
        /// Receipt items
        /// </summary>
        [Required]
        [JsonProperty("items")]
        public ICollection<ReceiptItem> Items { get; set; }

        // TODO: Add other fields

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if ((Items?.Count ?? 0) == 0)
            {
                yield return new ValidationResult("At least one receipt item required");
            }
        }
    }
}
