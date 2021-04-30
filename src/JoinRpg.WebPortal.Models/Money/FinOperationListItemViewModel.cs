using System;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Validation;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models
{
    public class FinOperationListItemViewModel
    {
        [Display(Name = "# операции")]
        public int FinanceOperationId { get; }

        [Display(Name = "Внесено денег"), Required]
        public int Money { get; }

        [Display(Name = "Изменение взноса"), Required]
        public int FeeChange { get; }

        [Display(Name = "Оплачено мастеру")]
        public User PaymentMaster { get; }

        [Display(Name = "Способ оплаты"), Required]
        public string PaymentTypeName { get; }

        [Display(Name = "Отметил"), Required]
        public User MarkingMaster { get; }

        [Display(Name = "Дата внесения"), Required, DateShouldBeInPast]
        public DateTime OperationDate { get; }

        [Display(Name = "Заявка"), Required]
        public string Claim { get; }

        [Url, Display(Name = "Ссылка на заявку")]
        public string ClaimLink { get; }

        [Display(Name = "Игрок"), Required]
        public User Player { get; }

        public FinOperationListItemViewModel(FinanceOperation fo, IUriService uriService)
        {
            PaymentTypeName = fo.PaymentType?.GetDisplayName();
            PaymentMaster = fo.PaymentType?.User;
            Claim = fo.Claim.Name;
            FeeChange = fo.FeeChange;
            Money = fo.MoneyAmount;
            OperationDate = fo.OperationDate;
            FinanceOperationId = fo.CommentId;
            MarkingMaster = fo.Comment.Author;
            Player = fo.Claim.Player;
            ClaimLink = uriService.Get(fo.Claim);

            if (fo.OperationType == FinanceOperationType.TransferFrom ||
                fo.OperationType == FinanceOperationType.TransferTo)
            {
                PaymentTypeName =
                    ((FinanceOperationTypeViewModel)fo.OperationType).GetDisplayName();
            }
        }
    }
}
