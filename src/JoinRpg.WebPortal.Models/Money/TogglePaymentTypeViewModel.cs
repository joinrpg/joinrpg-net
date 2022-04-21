using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models;

public class TogglePaymentTypeViewModel : IValidatableObject
{
    public int ProjectId { get; set; }

    public int? PaymentTypeId { get; set; }

    public PaymentTypeKindViewModel? TypeKind { get; set; }

    public int? MasterId { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PaymentTypeId == null && (TypeKind == null || (TypeKind != PaymentTypeKindViewModel.Online && MasterId == null)))
        {
            yield return new ValidationResult("Для новых методов оплаты должен быть указан тип и пользователь",
                new[] { nameof(PaymentTypeId), nameof(TypeKind), nameof(MasterId) });
        }
    }
}
