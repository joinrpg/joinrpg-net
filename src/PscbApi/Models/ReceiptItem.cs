// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace PscbApi.Models
{

    /// <summary>
    /// Receipt item
    /// </summary>
    public class ReceiptItem : IValidatableObject
    {
        /// <summary>
        /// Minimal acceptable price of an item
        /// </summary>
        public const decimal MinPrice = 0.01M;

        /// <summary>
        /// Position name
        /// </summary>
        [Required]
        [MaxLength(64)]
        [JsonProperty("text")]
        public string Name { get; set; }

        /// <summary>
        /// Price (including VAT)
        /// </summary>
        [JsonProperty("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Number of items
        /// </summary>
        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Total for current item
        /// </summary>
        /// <remarks>
        /// Could be less than <see cref="Price"/> * <see cref="Quantity"/>.
        /// If so, difference is a discount for current item 
        /// </remarks>
        [JsonProperty("amount")]
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// VAT system applied. See <see cref="VatSystemType"/> for details
        /// </summary>
        [JsonProperty("tax")]
        public VatSystemType VatType { get; set; } = VatSystemType.None;

        /// <summary>
        /// Payment type
        /// </summary>
        [JsonProperty("type")]
        public ItemPaymentType PaymentType { get; set; } = ItemPaymentType.FullPrepayment;

        /// <summary>
        /// Object type
        /// </summary>
        [JsonProperty("object")]
        public PaymentObjectType ObjectType { get; set; } = PaymentObjectType.Unclassified;

        /// <summary>
        /// Measurement unit of an item
        /// </summary>
        [JsonProperty("unit", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Unit { get; set; }

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Price < MinPrice)
            {
                yield return new ValidationResult($"{nameof(Price)} could not be less than {MinPrice}", new[] { nameof(Price) });
            }

            if (Quantity < 1)
            {
                yield return new ValidationResult($"{nameof(Quantity)} could not be less than 1", new[] { nameof(Quantity) });
            }

            if (TotalPrice < MinPrice)
            {
                yield return new ValidationResult($"{nameof(TotalPrice)} could not be less than {MinPrice}", new[] { nameof(TotalPrice) });
            }
        }
    }
}
