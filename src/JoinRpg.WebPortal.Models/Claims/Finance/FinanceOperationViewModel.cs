using JoinRpg.DataModel;
using JoinRpg.Domain;
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

    public User LinkedClaimUser { get; }

    public bool ShowLinkedClaimLinkIfTransfer { get; }

    public bool IsVisible { get; }

    public bool CanRefund { get; }

    public bool CanUpdate { get; }

    public bool CanPay { get; }

    /// <summary>
    /// External url (like bank payments system) to check this payment details
    /// </summary>
    public string? ExternalUrl { get; init; }

    public bool IsTransfer => OperationType is FinanceOperationTypeViewModel.TransferFrom or FinanceOperationTypeViewModel.TransferTo;

    public FinanceOperationViewModel(Claim claim, FinanceOperation source, bool isMaster, bool isPlayer)
    {
        Id = source.CommentId;
        ClaimId = source.ClaimId;
        ProjectId = source.ProjectId;
        Money = source.MoneyAmount;
        LinkedClaimId = source.LinkedClaimId;
        LinkedClaimName = source.LinkedClaim?.Name ?? "";
        LinkedClaimUser = source.LinkedClaim?.Player!;
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

        if (source.RecurrentPayment is not null)
        {
            Title = $"{Title} (подписка от {source.RecurrentPayment.CreateDate:d})";
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
            case FinanceOperationTypeViewModel.Refund when source.State == FinanceOperationState.Approved:
                Description = OperationState.GetShortName() ?? "";
                break;
        }

        CanUpdate = source is { State: FinanceOperationState.Proposed, OperationType: FinanceOperationType.Online or FinanceOperationType.Refund };
        CanRefund = isMaster
            && source is { State: FinanceOperationState.Approved, OperationType: FinanceOperationType.Online }
            && claim.FinanceOperations.All(fo => fo.RefundedOperationId != source.CommentId || fo.State is not (FinanceOperationState.Approved or FinanceOperationState.Proposed));
        CanPay = isPlayer
            && source is { State: FinanceOperationState.Proposed, BankDetails.QrCodeLink: not null };
    }
}
