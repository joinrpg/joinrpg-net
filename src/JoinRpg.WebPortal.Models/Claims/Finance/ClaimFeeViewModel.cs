using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Markdown;
using JoinRpg.Web.Claims;
using JoinRpg.Web.Models.Accommodation;

namespace JoinRpg.Web.Models;

public class ClaimFeeViewModel
{
    /// <param name="claim">
    /// EF-сущность той же заявки — пока нужна списку финансовых операций, подпискам и условиям
    /// льготного взноса: их в снимке заявки нет (ADR013).
    /// </param>
    /// <param name="claimInCharacter">Снимок заявки, по нему считается разбивка взноса.</param>
    /// <param name="accommodation">
    /// Проживание заявки — тип и комната из снимка заявки и плана поселения (ADR022); <c>null</c>,
    /// если поселение в проекте выключено.
    /// </param>
    public ClaimFeeViewModel(
        Claim claim,
        ClaimInCharacter claimInCharacter,
        ClaimViewModel model,
        UserIdentification currentUserId,
        ProjectInfo projectInfo,
        Func<string?, string?> externalPaymentUrlFactory,
        ClaimAccommodationViewModel? accommodation)
    {
        ArgumentNullException.ThrowIfNull(claimInCharacter);

        var feeBreakdown = claimInCharacter.CalculateFeeBreakdown();

        Status = model.Status;

        BaseFeeInfo = feeBreakdown.BaseFeeSetting;
        BaseFee = feeBreakdown.BaseFee;
        HasBaseFee = feeBreakdown.HasBaseFee;

        AccommodationFee = feeBreakdown.AccommodationFee;
        RoomType = accommodation?.AccommodationType?.Name ?? "";
        RoomName = accommodation?.RoomName;

        FieldsFee = new()
        {
            [FieldBoundToViewModel.Character] = feeBreakdown.CharacterFields.Fee,
            [FieldBoundToViewModel.Claim] = feeBreakdown.ClaimFields.Fee,
        };
        FieldsWithFeeCount = new()
        {
            [FieldBoundToViewModel.Character] = feeBreakdown.CharacterFields.FieldsWithFeeCount,
            [FieldBoundToViewModel.Claim] = feeBreakdown.ClaimFields.FieldsWithFeeCount,
        };
        FieldsTotalFee = feeBreakdown.FieldsFee;
        HasFieldsWithFee = feeBreakdown.HasFieldsWithFee;

        CurrentTotalFee = feeBreakdown.TotalFee;
        CurrentFee = feeBreakdown.TotalFee;

        foreach (var s in Enum.GetValues<FinanceOperationState>())
        {
            Balance[s] = 0;
        }

        foreach (var fo in claim.FinanceOperations)
        {
            Balance[fo.State] += fo.MoneyAmount;
        }

        HasMasterAccess = projectInfo.HasMasterAccess(currentUserId);
        HasFeeAdminAccess = projectInfo.HasMasterAccess(currentUserId, Permission.CanManageMoney);

        PaymentTypes = [.. projectInfo
            .GetAvailablePaymentTypesForUser(currentUserId, new UserIdentification(claim.PlayerUserId))
            .Select(pt => new PaymentTypeViewModel(pt))];

        PreferentialFeeEnabled = projectInfo.ProjectFinanceSettings.PreferentialFeeEnabled;
        PreferentialFeeUser = claimInCharacter.Claim.Finance.PreferentialFeeUser;
        PreferentialFeeConditions =
            ((MarkdownString?)claim.Project.Details.PreferentialFeeConditions).ToHtmlString();
        PreferentialFeeRequestEnabled = PreferentialFeeEnabled && !PreferentialFeeUser && Status.IsActive();

        ClaimId = claimInCharacter.ClaimId.ClaimId;
        ProjectId = claimInCharacter.ClaimId.ProjectId;
        FeeVariants = projectInfo.ProjectFinanceSettings.FeeSchedule
            .Select(f => f.Fee)
            .Append(CurrentFee)
            .Order()
            .Distinct()
            .ToList();
        FinanceOperations = claim.FinanceOperations
            .Select(
                fo => new FinanceOperationViewModel(claim, fo, model.HasMasterAccess, model.IsMyClaim)
                {
                    ExternalUrl = HasFeeAdminAccess ? externalPaymentUrlFactory(fo.BankDetails?.BankOperationKey) : null
                });
        VisibleFinanceOperations = FinanceOperations
            .Where(fo => fo.IsVisible);

        ShowOnlinePaymentControls = PaymentTypes.OnlinePaymentsEnabled() && model.IsMyClaim;
        HasSubmittablePaymentTypes = PaymentTypes.Any(pt => pt.TypeKind is PaymentTypeKindViewModel.Custom or PaymentTypeKindViewModel.Cash);

        // Determining payment status
        PaymentStatus = FinanceExtensions.GetClaimPaymentStatus(CurrentTotalFee, CurrentBalance);

        ShowRecurrentPaymentControls = PaymentTypes.RecurrentPaymentsEnabled() && model.IsMyClaim;
        RecurrentPayments = claim.RecurrentPayments
            .Select(e => new RecurrentPaymentViewModel(this, e))
            .OrderBy(static e => e.CreatedAt)
            .ToArray();
        CanMakeNewSubscription = ShowRecurrentPaymentControls
            && RecurrentPayments.All(rp => rp.Status is RecurrentPaymentStatusViewModel.Cancelled or RecurrentPaymentStatusViewModel.Failed);
        CanCancelSubscription = ShowRecurrentPaymentControls
            && RecurrentPayments.Any(rp => rp.Status is RecurrentPaymentStatusViewModel.Active
                || (HasFeeAdminAccess && rp.Status is RecurrentPaymentStatusViewModel.Created or RecurrentPaymentStatusViewModel.Cancelling));
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
    public ProjectFeeSettingInfo? BaseFeeInfo { get; }

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
    /// Комната, в которой живёт группа заявки, или <c>null</c>, если ещё не расселена.
    /// </summary>
    /// <remarks>
    /// Раньше здесь стояла пустая строка вместо <c>null</c>, и строка взноса за проживание у
    /// нерасселённой заявки выводилась с пустым «, комната ».
    /// </remarks>
    public string? RoomName { get; }

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
    /// Способы оплаты, которые текущий пользователь может выбрать в этой заявке.
    /// </summary>
    public IReadOnlyCollection<PaymentTypeViewModel> PaymentTypes { get; }

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
    /// true when a user can request preferential fee
    /// </summary>
    public bool PreferentialFeeRequestEnabled { get; }

    public bool ShowRecurrentPaymentControls { get; }

    /// <summary>
    /// true when a user can make new subscription (no active subscriptions)
    /// </summary>
    public bool CanMakeNewSubscription { get; }

    /// <summary>
    /// true when a user can cancel at least one subscription
    /// </summary>
    public bool CanCancelSubscription { get; }

    public IReadOnlyCollection<RecurrentPaymentViewModel> RecurrentPayments { get; }

    public int ClaimId { get; }
    public int ProjectId { get; }
    public IEnumerable<int> FeeVariants { get; }
}
