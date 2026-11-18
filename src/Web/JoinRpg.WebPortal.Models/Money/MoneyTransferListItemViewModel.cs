using JoinRpg.Common.WebComponents;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.Claims.Finance;
using JoinRpg.Web.Models.Money;

namespace JoinRpg.Web.Models;

public class MoneyTransferListItemViewModel : MoneyTransferViewModelBase
{
    [Display(Name = "Внес"), Required]
    public UserLinkViewModel MarkingMaster { get; }

    [Display(Name = "Статус")]
    public MoneyTransferStateViewModel State { get; }

    [Display(Name = "От")]
    public UserLinkViewModel Sender { get; }

    [Display(Name = "Кому")]
    public UserLinkViewModel Receiver { get; }

    public bool HasApproveAccess { get; }

    public int Id { get; }
    [Display(Name = "Комментарий")]
    public JoinHtmlString Comment { get; }

    public MoneyTransferListItemViewModel(MoneyTransfer fo, ICurrentUserAccessor currentUserId, ProjectInfo projectInfo)
    {
        Id = fo.Id;
        ProjectId = fo.ProjectId;
        Sender = new(fo.Sender.ToUserInfoHeader());
        Receiver = new(fo.Receiver.ToUserInfoHeader());
        MarkingMaster = new(fo.CreatedBy.ToUserInfoHeader());
        OperationDate = DateOnly.FromDateTime(fo.OperationDate.UtcDateTime);
        State = (MoneyTransferStateViewModel)fo.ResultState;

        Money = fo.Amount;

        var isPendingSender = State == MoneyTransferStateViewModel.PendingForSender ||
                    State == MoneyTransferStateViewModel.PendingForBoth;

        var isPendingReceiver = State == MoneyTransferStateViewModel.PendingForReceiver ||
                          State == MoneyTransferStateViewModel.PendingForBoth;
        var isPendingAny = isPendingReceiver || isPendingSender;

        HasApproveAccess =
            (projectInfo.HasMasterAccess(currentUserId, Permission.CanManageMoney) &&
             isPendingAny)
            || (currentUserId.UserIdOrDefault == fo.SenderId && isPendingSender)
            || (currentUserId.UserIdOrDefault == fo.ReceiverId && isPendingReceiver);

        Comment = ((MarkdownString?)fo.TransferText.Text).ToHtmlString();
    }
}
