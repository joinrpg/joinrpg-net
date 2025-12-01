using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Finances;
using JoinRpg.PrimitiveTypes.Claims.Finances;

namespace JoinRpg.Domain;

public static class FinanceExtensions
{
    /// <summary>
    /// Returns project fee for a specified date for claim
    /// </summary>
    private static int ProjectFeeForDate(this Claim claim, DateTime? operationDate)
    {
        var projectFeeInfo = claim.Project.ProjectFeeInfo(operationDate ?? DateTime.UtcNow);
        return (claim.PreferentialFeeUser
                   ? projectFeeInfo?.PreferentialFee
                   : projectFeeInfo?.Fee) ?? 0;
    }

    /// <summary>
    /// Returns fee info object for a specified date
    /// </summary>
    private static ProjectFeeSetting? ProjectFeeInfo(this Project project,
        DateTime operationDate)
        => project.ProjectFeeSettings.Where(pfs => pfs.StartDate.Date <= operationDate.Date)
            .OrderByDescending(pfs => pfs.StartDate.Date).FirstOrDefault();

    /// <summary>
    /// Returns fee info object for today
    /// </summary>
    public static ProjectFeeSetting? ProjectFeeInfo(this Project project)
        => project.ProjectFeeInfo(DateTime.UtcNow);

    /// <summary>
    /// Returns total sum of claim fee and all finance operations
    /// </summary>
    private static int ClaimTotalFee(this Claim claim, DateTime operationDate, int? fieldsFee, ProjectInfo projectInfo)
        => claim.ClaimCurrentFee(operationDate, fieldsFee, projectInfo);

    /// <summary>
    /// Returns total sum of claim fee and all finance operations using current date
    /// </summary>
    [Obsolete("CalculateClaimBalance")]
    public static int ClaimTotalFee(this Claim claim, ProjectInfo projectInfo, int? fieldsFee = null)
        => claim.ClaimTotalFee(DateTime.UtcNow, fieldsFee, projectInfo);

    /// <summary>
    /// Returns base fee (taken from project settings or claim's property CurrentFee)
    /// </summary>
    public static int BaseFee(this Claim claim, ProjectInfo projectInfo, DateTime? operationDate = null)
        => claim.CurrentFee ?? claim.ProjectFeeForDate(operationDate);

    /// <summary>
    /// Returns actual fee for a claim (as a sum of claim fee and fields fee) using current date
    /// </summary>
    public static int ClaimCurrentFee(this Claim claim, int? fieldsFee, ProjectInfo projectInfo)
        => claim.ClaimCurrentFee(DateTime.UtcNow, fieldsFee, projectInfo);

    /// <summary>
    /// Returns actual fee for a claim (as a sum of claim fee and fields fee)
    /// </summary>
    private static int ClaimCurrentFee(this Claim claim, DateTime operationDate, int? fieldsFee, ProjectInfo projectInfo)
    {
        return claim.BaseFee(projectInfo, operationDate)
               + claim.ClaimFieldsFee(fieldsFee, projectInfo)
               + claim.ClaimAccommodationFee();
        /******************************************************************
         * If you want to add additional fee to a claim's fee,
         * append your value to the expression above.
         * Example:
         *     return claim.BaseFee(operationDate)
         *         + claim.ClaimFieldsFee(fieldsFee)
         *         + claim.ClaimAccommodationFee()
         *         + claim.SomeOtherBigFee();
         *****************************************************************/
    }

    /// <summary>
    /// Returns claim payment status from total fee and money balance
    /// </summary>
    public static ClaimPaymentStatus GetClaimPaymentStatus(int totalFee, int balance)
    {
        if (totalFee < balance)
        {
            return ClaimPaymentStatus.Overpaid;
        }
        else if (totalFee == balance)
        {
            return ClaimPaymentStatus.Paid;
        }
        else if (balance > 0)
        {
            return ClaimPaymentStatus.MoreToPay;
        }
        else
        {
            return ClaimPaymentStatus.NotPaid;
        }
    }

    /// <summary>
    /// Returns claim payment status from claim' data
    /// </summary>
    public static ClaimPaymentStatus PaymentStatus(this Claim claim, ProjectInfo projectInfo)
        => GetClaimPaymentStatus(claim.ClaimTotalFee(projectInfo), claim.ClaimBalance());

    /// <summary>
    /// Returns total sum of all money flow operations
    /// </summary>
    public static int GetPaymentSum(this Claim claim)
        => claim.FinanceOperations
            .Where(fo => fo.Approved && fo.MoneyFlowOperation)
            .Sum(fo => fo.MoneyAmount);

    /// <summary>
    /// Returns current fee of a field with value
    /// </summary>
    public static int GetCurrentFee(this FieldWithValue self)
    {
        if (!self.Field.SupportsPricing)
        {
            return 0;
        }
        return self.Field.Type
        switch
        {
            ProjectFieldType.Checkbox => self.HasEditableValue ? self.Field.Price : 0,
            ProjectFieldType.Number => self.ToInt() * self.Field.Price,
            ProjectFieldType.Dropdown => self.GetDropdownValues().Sum(v => v.Price),
            ProjectFieldType.MultiSelect => self.GetDropdownValues().Sum(v => v.Price),

            _ => throw new NotSupportedException("Can't calculate pricing"),
        };
    }

    /// <summary>
    /// Calculates total fields fee
    /// </summary>
    private static int CalcClaimFieldsFee(this Claim claim, ProjectInfo projectInfo)
    {
        return claim.GetFields(projectInfo).Sum(f => f.GetCurrentFee());
    }

    /// <summary>
    /// Returns actual total claim fields fee
    /// </summary>
    private static int ClaimFieldsFee(this Claim claim, int? fieldsFee, ProjectInfo projectInfo)
    {
        if (fieldsFee == null)
        {
            fieldsFee = claim.FieldsFee ?? claim.CalcClaimFieldsFee(projectInfo);
        }
        // cache
        claim.FieldsFee = fieldsFee;

        return fieldsFee ?? 0;
    }

    /// <summary>
    /// Returns accommodation fee
    /// </summary>
    public static int ClaimAccommodationFee(this Claim claim)
        => claim.AccommodationRequest?.AccommodationType?.Cost ?? 0;

    /// <summary>
    /// Returns how many money left to pay
    /// </summary>
    [Obsolete("CalculateClaimBalance")]
    public static int ClaimFeeDue(this Claim claim, ProjectInfo projectInfo)
        => claim.ClaimTotalFee(projectInfo) - claim.ClaimBalance();

    public static ClaimBalance CalculateClaimBalance(this Claim claim, ProjectInfo projectInfo, DateTime? date = null)
    {
        var paid = claim.ApprovedFinanceOperations.Sum(fo => fo.MoneyAmount);
        var total = claim.ClaimTotalFee(date ?? DateTime.UtcNow, null, projectInfo);
        return new ClaimBalance(paid, total);
    }

    public static ClaimBalance CalculateClaimBalance(this UgClaim claim, ProjectInfo projectInfo, DateTime? date = null)
    {
        var paid = claim.FeePaid;
        var total = claim.Claim.ClaimTotalFee(date ?? DateTime.UtcNow, null, projectInfo);
        return new ClaimBalance(paid, total);
    }

    /// <summary>
    /// Returns sum of all approved finance operations
    /// </summary>
    ///
    [Obsolete("CalculateClaimBalance")]
    public static int ClaimBalance(this Claim claim)
        => claim.ApprovedFinanceOperations.Sum(fo => fo.MoneyAmount);

    /// <summary>
    /// Returns sum of all unapproved finance operations
    /// </summary>
    public static int ClaimProposedBalance(this Claim claim)
        => claim.FinanceOperations.Sum(fo =>
            fo.State == FinanceOperationState.Proposed ? fo.MoneyAmount : 0);

    [Obsolete]
    public static void RequestModerationAccess(this FinanceOperation finance, int currentUserId)
    {
        if (!finance.Claim.HasAccess(currentUserId,
                acl => acl.CanManageMoney) &&
            finance.PaymentType?.UserId != currentUserId)
        {
            throw new NoAccessToProjectException(finance, currentUserId);
        }
    }

    [Obsolete("CalculateClaimBalance")]
    public static bool ClaimPaidInFull(this Claim claim, ProjectInfo projectInfo)
        => claim.ClaimBalance() >= claim.ClaimTotalFee(projectInfo);

    private static bool ClaimPaidInFull(this Claim claim, DateTime operationDate, ProjectInfo projectInfo)
        => claim.ClaimBalance() >= claim.ClaimTotalFee(operationDate.AddDays(-1), null, projectInfo);

    public static void UpdateClaimFeeIfRequired(this Claim claim, DateTime operationDate, ProjectInfo projectInfo)
    {
        if (claim.Project.ProjectFeeSettings.Any() //If project has fee 
            && claim.CurrentFee == null //and fee not already fixed for claim
            && claim.ClaimPaidInFull(operationDate, projectInfo) //and current fee is payed in full
        )
        {
            claim.CurrentFee = claim.ProjectFeeForDate(operationDate); //fix fee for claim
        }
    }

    public static IEnumerable<MoneyTransfer> Approved(
        this IEnumerable<MoneyTransfer> transfers)
        => transfers.Where(mt => mt.ResultState == MoneyTransferState.Approved);

    public static IEnumerable<MoneyTransfer> SendedByMaster(
        this IEnumerable<MoneyTransfer> transfers,
        User master) => transfers.Where(mt => mt.SenderId == master.UserId);

    public static IEnumerable<MoneyTransfer> ReceivedByMaster(
        this IEnumerable<MoneyTransfer> transfers,
        User master) => transfers.Where(mt => mt.ReceiverId == master.UserId);

    public static int SendedByMasterSum(this IReadOnlyCollection<MoneyTransfer> transfers,
        User master) => transfers.Approved().SendedByMaster(master).Sum(mt => -mt.Amount);

    public static int ReceivedByMasterSum(this IReadOnlyCollection<MoneyTransfer> transfers,
        User master) => transfers.Approved().ReceivedByMaster(master).Sum(mt => mt.Amount);
}
