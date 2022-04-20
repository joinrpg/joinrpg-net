using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models
{
    public enum PaymentMethodViewModel
    {
        [Display(Name = "Оплатить картой", Description = "Оплата банковской картой или при помощи систем Apply Pay, Google Pay, Samsung Pay")]
        BankCard = PaymentMethod.BankCard,

        [Display(Name = "Оплатить по QR-коду", Description = "Оплата через Систему Быстрых Платежей при помощи QR-кода. У вас должно быть установлено мобильное приложение вашего банка и оно должно поддерживать оплату по QR-коду.")]
        FastPaymentsSystem = PaymentMethod.FastPaymentsSystem,
    }

    public class PaymentViewModel : PaymentViewModelBase
    {
        /// <summary>
        /// For online payments, comment text is not actually required
        /// </summary>
        [DisplayName("Комментарий к платежу")]
        public new string CommentText
        {
            get => base.CommentText;
            set => base.CommentText = value;
        }

        [Range(1, 100000, ErrorMessage = "Сумма оплаты должна быть от 1 до 100000")]
        [Required]
        [DisplayName("Сумма к оплате")]
        public new int Money
        {
            get => base.Money;
            set => base.Money = value;
        }

        public bool AcceptContract { get; set; }

        public PaymentMethodViewModel Method { get; set; }

        public PaymentViewModel() { }

        public PaymentViewModel(ClaimViewModel claim) : base(claim) => ActionName = "Оплатить";

        /// <inheritdoc />
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            => base.Validate(validationContext)
                .AppendIf(
                    !AcceptContract,
                    () => new ValidationResult(
                        "Необходимо принять соглашение для проведения оплаты",
                        new[] { nameof(AcceptContract) }));
    }
}
