using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers.Web;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.Models;

public class ClaimFeeViewModel
{
    public ClaimFeeViewModel(Claim claim, ClaimViewModel model, int currentUserId, ProjectInfo projectInfo)
    {
        Status = model.Status;

        // Reading project fee info applicable for today
        BaseFeeInfo = claim.CurrentFee == null ? claim.Project.ProjectFeeInfo() : null;
        // Reading base fee of a claim
        BaseFee = claim.BaseFee();
        // Checks for base fee availability
        HasBaseFee = BaseFeeInfo != null || claim.CurrentFee != null;

        AccommodationFee = claim.ClaimAccommodationFee();
        RoomType = claim.AccommodationRequest?.AccommodationType.Name ?? "";
        RoomName = claim.AccommodationRequest?.Accommodation?.Name ?? "";

        FieldsWithFeeCount = model.Fields.FieldWithFeeCount;
        FieldsTotalFee = model.Fields.FieldsTotalFee;

        HasFieldsWithFee = model.Fields.HasFieldsWithFee;
        CurrentTotalFee = claim.ClaimTotalFee(projectInfo, FieldsTotalFee);
        CurrentFee = claim.ClaimCurrentFee(FieldsTotalFee, projectInfo);
        FieldsFee = model.Fields.FieldsFee;

        foreach (var s in Enum.GetValues<FinanceOperationState>())
        {
            Balance[s] = 0;
        }

        foreach (var fo in claim.FinanceOperations)
        {
            Balance[fo.State] += fo.MoneyAmount;
        }

        HasMasterAccess = claim.HasMasterAccess(currentUserId);
        HasFeeAdminAccess = claim.HasAccess(currentUserId, acl => acl.CanManageMoney);

        PreferentialFeeEnabled = claim.Project.Details.PreferentialFeeEnabled;
        PreferentialFeeUser = claim.PreferentialFeeUser;
        PreferentialFeeConditions =
            claim.Project.Details.PreferentialFeeConditions.ToHtmlString();
        PreferentialFeeRequestEnabled = PreferentialFeeEnabled && !PreferentialFeeUser && Status.IsActive();

        ClaimId = claim.ClaimId;
        ProjectId = claim.ProjectId;
        FeeVariants = claim.Project.ProjectFeeSettings
            .Select(f => f.Fee)
            .Append(CurrentFee)
            .OrderBy(x => x)
            .ToList();
        FinanceOperations = claim.FinanceOperations
            .Select(fo => new FinanceOperationViewModel(fo, model.HasMasterAccess));
        VisibleFinanceOperations = FinanceOperations
            .Where(fo => fo.IsVisible);

        ShowOnlinePaymentControls = model.PaymentTypes.OnlinePaymentsEnabled() && currentUserId == claim.PlayerUserId;
        HasSubmittablePaymentTypes = model.PaymentTypes.Any(pt => pt.TypeKind is PaymentTypeKindViewModel.Custom or PaymentTypeKindViewModel.Cash);

        // Determining payment status
        PaymentStatus = FinanceExtensions.GetClaimPaymentStatus(CurrentTotalFee, CurrentBalance);

        ShowRecurrentPaymentControls = model.PaymentTypes.RecurrentPaymentsEnabled() && currentUserId == claim.PlayerUserId;
        RecurrentPayments = claim.RecurrentPayments
            .Select(e => new RecurrentPaymentViewModel(this, e))
            .OrderBy(static e => e.CreatedAt)
            .ToArray();
    }

    /// <summary>
    /// Claim status taken from claim view model
    /// </summary>
    public ClaimFullStatusView Status { get; }

    /// <summary>
    /// Claim fee taken from project settings or defined manually
    /// </summary>
    public int BaseFee { get; }

    /// <summary>
    /// Claim
    /// </summary>
    public ProjectFeeSetting? BaseFeeInfo { get; }

    /// <summary>
    /// true if there is any base fee for this claim
    /// </summary>
    public bool HasBaseFee { get; }

    /// <summary>
    /// Sum of fields fees
    /// </summary>
    public int FieldsTotalFee { get; }

    /// <summary>
    /// BaseFee + FieldsTotalFee
    /// </summary>
    public int CurrentFee { get; }

    /// <summary>
    /// Accommodation fee
    /// </summary>
    public int AccommodationFee { get; }

    /// <summary>
    /// Name of choosen room type
    /// </summary>
    public string RoomType { get; }

    /// <summary>
    /// Number or name of occupied room
    /// </summary>
    public string RoomName { get; }

    /// <summary>
    /// Fields fee, separated by bound
    /// </summary>
    public Dictionary<FieldBoundToViewModel, int> FieldsFee { get; }

    /// <summary>
    /// true if fee row should be visible in claim editor.
    /// One of the following conditions has to be met:
    /// CurrentBalance > 0 (player sends some money),
    /// or there is any base fee (assigned manually or automatically),
    /// or there is at least one field with fee
    /// </summary>
    public bool ShowFee
        => CurrentBalance > 0 || HasBaseFee || HasFieldsWithFee;

    /// <summary>
    /// Returns count of fields with assigned fee
    /// </summary>
    public Dictionary<FieldBoundToViewModel, int> FieldsWithFeeCount { get; }

    public bool HasAccommodationFee
        => AccommodationFee != 0;

    /// <summary>
    /// Returns true if there is at least one field with fee
    /// </summary>
    public bool HasFieldsWithFee { get; }

    /// <summary>
    /// Sum of basic fee, total fields fee and finance operations
    /// </summary>
    public int CurrentTotalFee { get; }

    /// <summary>
    /// Sums of all finance operations by type
    /// </summary>
    public readonly Dictionary<FinanceOperationState, int> Balance = new();

    /// <summary>
    /// Sum of approved finance operations
    /// </summary>
    public int CurrentBalance => Balance[FinanceOperationState.Approved];

    public ClaimPaymentStatus PaymentStatus { get; }

    /// <summary>
    /// List of associated payment operations
    /// </summary>
    public IEnumerable<FinanceOperationViewModel> FinanceOperations { get; }

    /// <summary>
    /// List of finance operations to be displayed in payments list
    /// </summary>
    public IEnumerable<FinanceOperationViewModel> VisibleFinanceOperations { get; }

    /// <summary>
    /// true if online payment enabled
    /// </summary>
    public bool ShowOnlinePaymentControls { get; }

    /// <summary>
    /// true if there is any payment type(s) except online
    /// </summary>
    public bool HasSubmittablePaymentTypes { get; }

    public bool HasMasterAccess { get; }

    public bool HasFeeAdminAccess { get; }

    public bool PreferentialFeeEnabled { get; }
    public bool PreferentialFeeUser { get; }
    public JoinHtmlString PreferentialFeeConditions { get; }

    /// <summary>
    /// true if a user can request preferential fee
    /// </summary>
    public bool PreferentialFeeRequestEnabled { get; }

    public bool ShowRecurrentPaymentControls { get; }

    public IReadOnlyCollection<RecurrentPaymentViewModel> RecurrentPayments { get; }

    public int ClaimId { get; }
    public int ProjectId { get; }
    public IEnumerable<int> FeeVariants { get; }
}
