using JoinRpg.Helpers.Validation;

namespace JoinRpg.Web.Models.Money;

public abstract class MoneyTransferViewModelBase : IProjectIdAware
{
    [Display(Name = "Сумма денег"), Required, Range(1, 1000000)]
    public int Money { get; set; }

    [Display(Name = "Дата перевода"), Required, DateShouldBeInPast]
    public DateOnly OperationDate { get; set; }

    public int ProjectId { get; set; }
}


public class CreateMoneyTransferViewModel : MoneyTransferViewModelBase, IValidatableObject
{
    public bool HasAdminAccess { get; set; }

    [Display(Name = "От")]
    public UserIdentification Sender { get; set; } = null!;

    [Display(Name = "Кому")]
    public UserIdentification Receiver { get; set; } = null!;

    [Required(ErrorMessage = "Заполните текст комментария"),
     Display(
         Name = "Текст комментария",
         Description = "Укажите при каких обстоятельствах и когда вы передавали деньги. Например: на мастерской стрелке у такого-то дома, кинул на Тинькофф. Это нужно, чтобы противоположной стороне легче вспомнилось. "
         ),
     UIHint("MarkdownString")]
    public string CommentText { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Receiver == Sender)
        {
            yield return new ValidationResult("Нельзя принять деньги у самого себя",
                new[] { nameof(Sender), nameof(Receiver) });
        }
    }
}
