using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.Helpers.Web;
using JoinRpg.Markdown;
using JoinRpg.Web.Models.Money;

namespace JoinRpg.Web.Models
{
    public class MoneyTransferListItemViewModel : MoneyTransferViewModelBase
    {
        [Display(Name = "Внес"), Required]
        public User MarkingMaster { get; }

        [Display(Name = "Статус")]
        public MoneyTransferStateViewModel State { get; }

        [Display(Name = "От")]
        public User Sender { get; }

        [Display(Name = "Кому")]
        public User Receiver { get; }

        public bool HasApproveAccess { get; }

        public int Id { get; }
        [Display(Name = "Комментарий")]
        public JoinHtmlString Comment { get; }

        public MoneyTransferListItemViewModel(MoneyTransfer fo, int currentUserId)
        {
            Id = fo.Id;
            ProjectId = fo.ProjectId;
            Sender = fo.Sender;
            Receiver = fo.Receiver;
            MarkingMaster = fo.CreatedBy;
            OperationDate = fo.OperationDate.UtcDateTime;
            State = (MoneyTransferStateViewModel)fo.ResultState;

            Money = fo.Amount;
            MarkingMaster = fo.CreatedBy;

            var isPendingSender = State == MoneyTransferStateViewModel.PendingForSender ||
                        State == MoneyTransferStateViewModel.PendingForBoth;

            var isPendingReceiver = State == MoneyTransferStateViewModel.PendingForReceiver ||
                              State == MoneyTransferStateViewModel.PendingForBoth;
            var isPendingAny = isPendingReceiver || isPendingSender;

            HasApproveAccess =
                (fo.Project.HasMasterAccess(currentUserId, acl => acl.CanManageMoney) &&
                 isPendingAny)
                || (currentUserId == Sender.UserId && isPendingSender)
                || (currentUserId == Receiver.UserId && isPendingReceiver);

            Comment = fo.TransferText.Text.ToHtmlString();
        }
    }
}
