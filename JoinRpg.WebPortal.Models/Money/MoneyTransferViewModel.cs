using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Helpers.Validation;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models.Money
{
    public abstract class MoneyTransferViewModelBase : IProjectIdAware
    {
        [Display(Name = "Сумма денег"), Required, Range(1, 1000000)]
        public int Money { get; set; }

        [Display(Name = "Дата перевода"), Required, DateShouldBeInPast]
        public DateTime OperationDate { get; set; }

        public int ProjectId { get; set; }
    }


    public class CreateMoneyTransferViewModel : MoneyTransferViewModelBase, IValidatableObject
    {
        [ReadOnly(true)]
        public IEnumerable<MasterListItemViewModel> Masters { get; set; }

        public bool HasAdminAccess { get; set; }

        [Display(Name = "От")]
        public int Sender { get; set; }

        [Display(Name = "Кому")]
        public int Receiver { get; set; }

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
                    new[] {nameof(Sender), nameof(Receiver)});
            }
        }
    }
}
