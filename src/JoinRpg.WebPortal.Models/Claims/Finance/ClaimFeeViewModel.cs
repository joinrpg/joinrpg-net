using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Markdown;
using JoinRpg.Web.Models.Accommodation;

namespace JoinRpg.Web.Models;

public class ClaimFeeViewModel
{
    /// <param name="claim">
    /// EF-сущность той же заявки — пока нужна списку финансовых операций и подпискам: их в снимке
    /// заявки нет (ADR013).
    /// </param>
    /// <param name="claimInCharacter">
    /// Снимок заявки: по нему считается разбивка взноса, из него же берутся метаданные проекта,
    /// статус заявки и её игрок.
    /// </param>
    /// <param name="accommodation">
    /// Проживание заявки — тип и комната из снимка заявки и плана поселения (ADR022); <c>null</c>,
    /// если поселение в проекте выключено.
    /// </param>
    /// <param name="preferentialFeeConditions">
    /// Условия льготного взноса из доменного <c>ProjectDetails</c>. В <c>ProjectInfo</c> их нет — текст
    /// бывает большим (#5112), поэтому их читают только при включённой льготе; иначе <c>null</c>.
    /// </param>
    public ClaimFeeViewModel(
        Claim claim,
        ClaimInCharacter claimInCharacter,
        UserIdentification currentUser,
        Func<string?, string?> externalPaymentUrlFactory,
        ClaimAccommodationViewModel? accommodation,
        MarkdownString? preferentialFeeConditions)
    {
        ArgumentNullException.ThrowIfNull(claimInCharacter);

        var projectInfo = claimInCharacter.ProjectInfo;
        var claimData = claimInCharacter.Claim;
        var isMyClaim = claimData.PlayerId == currentUser;
        var feeBreakdown = claimInCharacter.CalculateFeeBreakdown();

        BaseFeeInfo = feeBreakdown.BaseFeeSetting;
        BaseFee = feeBreakdown.BaseFee;
        HasBaseFee = feeBreakdown.HasBaseFee;

        AccommodationFee = feeBreakdown.AccommodationFee;
        ProjectHasPaidAccommodation = projectInfo.AccommodationSettings.HasPaidTypes;
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

        HasMasterAccess = projectInfo.HasMasterAccess(currentUser);
        HasFeeAdminAccess = projectInfo.HasMasterAccess(currentUser, Permission.CanManageMoney);

        PaymentTypes = [.. projectInfo
            .GetAvailablePaymentTypesForUser(currentUser, claimData.PlayerId)
            .Select(pt => new PaymentTypeViewModel(pt))];

        PreferentialFeeEnabled = projectInfo.ProjectFinanceSettings.PreferentialFeeEnabled;
        PreferentialFeeUser = claimData.Finance.PreferentialFeeUser;
        PreferentialFeeConditions = preferentialFeeConditions.ToHtmlString();
        PreferentialFeeRequestEnabled = PreferentialFeeEnabled && !PreferentialFeeUser && claimData.IsActive;

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
                fo => new FinanceOperationViewModel(claim, fo, HasMasterAccess, isMyClaim)
                {
                    ExternalUrl = HasFeeAdminAccess ? externalPaymentUrlFactory(fo.BankDetails?.BankOperationKey) : null,
                    FinanceModeration = fo.RequireModeration
                        && projectInfo.CanModerateFinanceOperation(fo.GetPaymentTypeId(), currentUser)
                        ? fo.ToModerationView(projectInfo)
                        : null,
                });
        VisibleFinanceOperations = FinanceOperations
            .Where(fo => fo.IsVisible);

        ShowOnlinePaymentControls = PaymentTypes.OnlinePaymentsEnabled() && isMyClaim;
        HasSubmittablePaymentTypes = PaymentTypes.Any(pt => pt.TypeKind is PaymentTypeKindViewModel.Custom or PaymentTypeKindViewModel.Cash);

        // Determining payment status
        PaymentStatus = new ClaimBalance(FeePaid: CurrentBalance, TotalFee: CurrentTotalFee).PaymentStatus;

        ShowRecurrentPaymentControls = PaymentTypes.RecurrentPaymentsEnabled() && isMyClaim;
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
    /// В проекте включено поселение и есть платный тип проживания.
    /// </summary>
    public bool ProjectHasPaidAccommodation { get; }

    /// <summary>
    /// Показывать ли блок взноса в заявке — а с ним и кнопки «Отметить оплату…» и «Изменить взнос».
    /// Нужно одно из условий: уже есть принятые оплаты, есть базовый взнос (назначенный вручную
    /// или по графику), есть поля с оплатой или в проекте есть платное поселение.
    /// </summary>
    /// <remarks>
    /// Платное поселение учитывается, даже если в заявке проживание не выбрано: иначе в проекте
    /// без взноса оплату за проживание нельзя отметить, пока игрока не поселят (#5646).
    /// </remarks>
    public bool ShowFee
        => CurrentBalance > 0 || HasBaseFee || HasFieldsWithFee || ProjectHasPaidAccommodation;

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
    /// Есть операция, которая ждёт модерации и которую текущий пользователь вправе подтвердить или
    /// отклонить, — по ней диалог оплаты предупреждает о неразобранном платеже игрока.
    /// </summary>
    public bool HasModeratableFinanceOperations => FinanceOperations.Any(fo => fo.FinanceModeration is not null);

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
