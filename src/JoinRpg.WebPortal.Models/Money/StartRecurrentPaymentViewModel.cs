using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models;

public class StartRecurrentPaymentViewModel : PaymentViewModelBase
{
    /// <summary>
    /// For online payments, comment text is not actually required
    /// </summary>
    [DisplayName("Комментарий к подписке")]
    public new string? CommentText
    {
        get => base.CommentText;
        set => base.CommentText = value!;
    }

    [Range(100, 10000, ErrorMessage = "Сумма ежемесячного платежа должна быть от 100 до 10000")]
    [Required]
    [DisplayName("Сумма ежемесячного платежа")]
    public new int Money
    {
        get => base.Money;
        set => base.Money = value;
    }

    public bool AcceptContract { get; set; }

    public bool Update { get; set; }

    public PaymentMethodViewModel Method { get; set; } = PaymentMethodViewModel.FastPaymentsSystem;

    public StartRecurrentPaymentViewModel()
    { }

    public StartRecurrentPaymentViewModel(ClaimViewModel claim) : base(claim)
    {
        Update = claim.ClaimFee.RecurrentPayments.Any(rp => rp.Status == RecurrentPaymentStatusViewModel.Active);
        ActionName = Update ? "Изменить подписку" : "Оформить подписку";
        Money = claim.ClaimFee.RecurrentPayments.LastOrDefault(rp => rp.Status == RecurrentPaymentStatusViewModel.Active)?.Amount
            ?? claim.ClaimFee.BaseFee;
    }

    /// <inheritdoc />
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => base
            .Validate(validationContext)
            .AppendIf(
                !AcceptContract,
                () => new ValidationResult(
                    "Необходимо принять соглашение для проведения оплаты",
                    new[] { nameof(AcceptContract) }))
            .AppendIf(
                Method != PaymentMethodViewModel.FastPaymentsSystem,
                () => new ValidationResult(
                    "Оформление подписки поддерживается только через СБП",
                    new[] { nameof(Method) }));
}
