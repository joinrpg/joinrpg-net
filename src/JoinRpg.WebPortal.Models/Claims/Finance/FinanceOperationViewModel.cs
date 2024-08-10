using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Web.Models.Money;

namespace JoinRpg.Web.Models;

public class FinanceOperationViewModel
{

    public int Id { get; }

    public int ClaimId { get; }

    public string RowCssClass { get; }

    public string Title { get; }

    public int Money { get; }

    public string Description { get; } = "";

    public FinanceOperationTypeViewModel OperationType { get; }

    public FinanceOperationStateViewModel OperationState { get; }

    public string Date { get; }

    public int ProjectId { get; }

    public int? LinkedClaimId { get; }

    public string LinkedClaimName { get; }

    public int? RecurrentPaymentId { get; }

    public bool CheckPaymentState { get; }

    public User LinkedClaimUser { get; }

    public bool ShowLinkedClaimLinkIfTransfer { get; }

    public bool IsVisible { get; }

    public FinanceOperationViewModel(FinanceOperation source, bool isMaster)
    {
        Id = source.CommentId;
        ClaimId = source.ClaimId;
        ProjectId = source.ProjectId;
        Money = source.MoneyAmount;
        LinkedClaimId = source.LinkedClaimId;
        LinkedClaimName = LinkedClaimId.HasValue ? source.LinkedClaim.Name : "";
        LinkedClaimUser = source.LinkedClaim?.Player;
        OperationType = (FinanceOperationTypeViewModel)source.OperationType;
        OperationState = (FinanceOperationStateViewModel)source.State;
        RowCssClass = source.State.ToRowClass();
        Date = source.OperationDate.ToShortDateString();
        ShowLinkedClaimLinkIfTransfer = isMaster;
#pragma warning disable CS0612 // Type or member is obsolete
        IsVisible = OperationType != FinanceOperationTypeViewModel.FeeChange
#pragma warning restore CS0612 // Type or member is obsolete
                    && OperationType != FinanceOperationTypeViewModel.PreferentialFeeRequest;

        RecurrentPaymentId = source.RecurrentPaymentId;

        Title = OperationType.GetDescription() ?? "";
        if (string.IsNullOrWhiteSpace(Title))
        {
            Title = OperationType.GetDisplayName();
        }
        else
        {
            Title = string.Format(
                Title,
                OperationType.GetDisplayName(),
                source.PaymentType?.GetDisplayName());
        }

        switch (OperationType)
        {
            case FinanceOperationTypeViewModel.Submit when source.Approved:
            case FinanceOperationTypeViewModel.Submit when source.State == FinanceOperationState.Proposed:
                Description = OperationState.GetDisplayName();
                break;
            case FinanceOperationTypeViewModel.Online when source.Approved:
                Description = OperationState.GetShortName() ?? "";
                break;
            case FinanceOperationTypeViewModel.Online when source.State == FinanceOperationState.Invalid:
                Description = OperationState.GetDisplayName();
                break;
            case FinanceOperationTypeViewModel.Online when source.State == FinanceOperationState.Proposed:
                CheckPaymentState = true;
                break;
        }
    }
}
