using System.Collections.Immutable;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Helpers;
using PscbApi;
using PscbApi.Models;

namespace JoinRpg.Services.Interfaces;

/// <summary>
/// Payment methods to be used for making payments
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Traditional payments using bank cards, Samsung pay, Apple pay...
    /// </summary>
    BankCard,

    /// <summary>
    /// Payments using Fast Payments System via QR Code (Russia only)
    /// </summary>
    FastPaymentsSystem,
}

/// <summary>
/// Base class for payment requests
/// </summary>
public class PaymentRequest
{
    /// <summary>
    /// Database Id of a payer
    /// </summary>
    public int PayerId { get; set; }

    /// <summary>
    /// How much to pay
    /// </summary>
    public int Money { get; set; }

    /// <summary>
    /// Payment method to use
    /// </summary>
    public PaymentMethod Method { get; set; }

    /// <summary>
    /// Comment added by payer
    /// </summary>
    public string? CommentText { get; set; }

    /// <summary>
    /// Date and time of the operation
    /// </summary>
    public DateTime OperationDate { get; set; }
}

/// <summary>
/// Payment request for game fee
/// </summary>
public class ClaimPaymentRequest : PaymentRequest
{
    /// <summary>
    /// Database Id of a project
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// Database Id of a claim
    /// </summary>
    public int ClaimId { get; set; }

    /// <summary>
    /// true to initiate an original recurrent payment and create a new instance of the <see cref="RecurrentPayment"/> class
    /// </summary>
    public bool Recurrent { get; set; }

    /// <summary>
    /// Id of a <see cref="RecurrentPayment"/> when new payment must be derived from an exited recurrent payment.
    /// </summary>
    public int? FromRecurrentPaymentId { get; set; }

    /// <summary>
    /// true to initiate a refund. Not compatible with <see cref="Recurrent"/>
    /// </summary>
    public bool Refund { get; set; }

    /// <summary>
    /// When <see cref="Refund"/> is true, must contain Id of a <see cref="FinanceOperation"/> to refund.
    /// </summary>
    public int? FinanceOperationToRefundId { get; set; }
}

/// <summary>
/// Base class for payment results
/// </summary>
public class PaymentContext
{
    /// <summary>
    /// true if payment was accepted
    /// </summary>
    public bool Accepted { get; set; }

    /// <summary>
    /// Payment request descriptor built by bank API
    /// </summary>
    public PaymentRequestDescriptor RequestDescriptor { get; set; }
}

/// <summary>
/// Result of claim payment
/// </summary>
public class ClaimPaymentContext : PaymentContext;

/// <summary>
/// Base class for payment results
/// </summary>
public class PaymentResultContext
{
    /// <summary>
    /// true if payment was successfully made
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Database Id of a finance operation
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Bank response info
    /// </summary>
    public BankResponseInfo BankResponse { get; set; }
}

public class FastPaymentsSystemBank : FpsBank
{
    public static readonly ImmutableArray<string> ParasitePrefixes = ["Банк ", "АК ", "АБ ", "АКБ ", "ПНКО ", "КБ ", "НКО ", "СКБ ", "УКБ ", "РНКО ", "ИКБР "];

    public string Id { get; }

    public string ClearName { get; }

    public string First1 { get; }

    public string First2 { get; }

    public string First3 { get; }

    public string First4 { get; }

    public FastPaymentsSystemBank(FpsBank source, int index)
    {
        Id = $"bank{index}";
        Name = source.Name;
        LogoUrl = source.LogoUrl;
        PaymentUrl = source.PaymentUrl;

        ClearName = Name.RemoveFromString(ParasitePrefixes, StringComparison.InvariantCultureIgnoreCase).Trim().ToLowerInvariant();
        First1 = ClearName.Substring(0, 1);
        First2 = ClearName.Substring(0, 2);
        First3 = ClearName.Substring(0, 3);
        First4 = ClearName.Length >= 4 ? ClearName.Substring(0, 4) : First3;
    }
}

public class FastPaymentsSystemMobilePaymentContext
{
    public string QrCodeUrl { get; set; }

    public int Amount { get; set; }

    public string Details { get; set; }

    public int ProjectId { get; set; }

    public int ClaimId { get; set; }

    public int OperationId { get; set; }

    public FpsPlatform ExpectedPlatform { get; set; }

    public IReadOnlyCollection<FastPaymentsSystemBank> TopBanks { get; }

    public IReadOnlyCollection<FastPaymentsSystemBank> AllBanks { get; }

    public FastPaymentsSystemMobilePaymentContext(ICollection<FpsBank>? banks, int top = 5)
    {
        if (banks?.Count > 0)
        {
            TopBanks = banks
                .Take(top)
                .Select(static (bank, index) => new FastPaymentsSystemBank(bank, index + 100000))
                .ToArray();

            AllBanks = banks
                .Select(static (bank, index) => new FastPaymentsSystemBank(bank, index))
                .OrderBy(static bank => bank.ClearName)
                .ToArray();
        }
        else
        {
            TopBanks = [];
            AllBanks = [];
        }
    }
}

/// <summary>
/// Payments service
/// </summary>
public interface IPaymentsService
{
    /// <summary>
    /// Creates finance operation and returns payment context
    /// </summary>
    /// <param name="request">Payment request</param>
    Task<ClaimPaymentContext> InitiateClaimPaymentAsync(ClaimPaymentRequest request);

    /// <summary>
    /// Creates finance operation and returns payment context for the Fast Payments System mobile payment.
    /// </summary>
    /// <param name="request">Payment request</param>
    /// <param name="platform">Desired platform</param>
    Task<FastPaymentsSystemMobilePaymentContext> InitiateFastPaymentsSystemMobilePaymentAsync(ClaimPaymentRequest request, FpsPlatform platform);

    Task<FastPaymentsSystemMobilePaymentContext> GetFastPaymentsSystemMobilePaymentContextAsync(
        int projectId,
        int claimId,
        int operationId,
        FpsPlatform platform);

    /// <summary>
    /// Updates status of a proposed payment
    /// </summary>
    /// <param name="projectId">Database Id of a project</param>
    /// <param name="claimId">Database Id of a claim</param>
    /// <param name="orderId">Finance operation Id</param>
    Task<FinanceOperationState> UpdateClaimPaymentAsync(int projectId, int claimId, int orderId);

    /// <summary>
    /// Updates status of the last proposed payment
    /// </summary>
    /// <param name="projectId">Database Id of a project</param>
    /// <param name="claimId">Database Id of a claim</param>
    Task UpdateLastClaimPaymentAsync(int projectId, int claimId);

    /// <summary>
    /// Updates a provided finance operation
    /// </summary>
    /// <param name="fo">Finance operation to update</param>
    /// <returns>State of the provided finance operation after the update</returns>
    Task<FinanceOperationState> UpdateFinanceOperationAsync(FinanceOperation fo);

    /// <summary>
    /// Sets recurrent payment to cancelled internally and tries to cancel on the bank side.
    /// </summary>
    /// <param name="projectId">Id of a project</param>
    /// <param name="claimId">Id of a claim</param>
    /// <param name="recurrentPaymentId">Id of a recurrent payment</param>
    /// <returns>true when payment was successfully cancelled, false otherwise, null when it was not even possible to start</returns>
    Task<bool?> CancelRecurrentPaymentAsync(int projectId, int claimId, int recurrentPaymentId);

    /// <summary>
    /// Tries to charge <paramref name="amount"/> money using payment method assigned with a specified recurrent payment.
    /// </summary>
    /// <param name="projectId">Id of a project</param>
    /// <param name="claimId">Id of a claim</param>
    /// <param name="recurrentPaymentId">Id of a recurrent payment</param>
    /// <param name="amount">How much money to charge. If null, will be taken as much as was charged in the first payment.</param>
    /// <param name="internalCall">When true, access will not be verified.</param>
    /// <returns>An instance of the <see cref="FinanceOperation"/> or null when it was even not possible to initiate operation.</returns>
    /// <remarks>
    /// Payments are typically asynchronous operations. It is necessary to update its state
    /// a little bit later using <see cref="UpdateClaimPaymentAsync"/>.
    /// </remarks>
    Task<FinanceOperation?> PerformRecurrentPaymentAsync(int projectId, int claimId, int recurrentPaymentId, int? amount, bool internalCall = false);

    /// <summary>
    /// Tries to charge <paramref name="amount"/> money using payment method assigned with a specified recurrent payment.
    /// </summary>
    /// <param name="recurrentPayment">Data of recurrent payment to perform. It must have <see cref="RecurrentPayment.Claim"/>,
    /// <see cref="RecurrentPayment.Project"/> and <see cref="RecurrentPayment.PaymentType"/> properties initialized
    /// with valid data.</param>
    /// <param name="amount">How much money to charge. If null, will be taken as much as was charged in the first payment.</param>
    /// <param name="internalCall">When true, access will not be verified.</param>
    /// <returns>An instance of the <see cref="FinanceOperation"/> or null when it was even not possible to initiate operation.</returns>
    /// <remarks>
    /// Payments are typically asynchronous operations. It is necessary to update its state
    /// a little bit later using <see cref="UpdateClaimPaymentAsync"/>.
    /// </remarks>
    Task<FinanceOperation?> PerformRecurrentPaymentAsync(RecurrentPayment recurrentPayment, int? amount, bool internalCall = false);

    /// <summary>
    /// Tries to make a refund of a specified payment.
    /// </summary>
    /// <param name="projectId">Id of a project</param>
    /// <param name="claimId">Id of a claim</param>
    /// <param name="operationId">Id of a payment to refund.</param>
    /// <returns>An instance of the <see cref="FinanceOperation"/> that represents the refund.</returns>
    Task<FinanceOperation> RefundAsync(int projectId, int claimId, int operationId);

    /// <summary>
    /// Tries to create an url to open payment details in external system using the external payment key of a payment.
    /// </summary>
    /// <param name="externalPaymentKey">A key returned from an external payment system.</param>
    /// <returns></returns>
    string? GetExternalPaymentUrl(string? externalPaymentKey);

    /// <summary>
    /// Searches for recurrent payments using the key-set pagination.
    /// </summary>
    /// <param name="afterId">Takes recurrent payments with primary keys greater than provided value.</param>
    /// <param name="activityStatus">Determines how to treat activity of a recurrent payment.
    ///     Active recurrent payment is that payment which has the <see cref="RecurrentPaymentStatus.Active"/> status,
    ///     and its project is active, its claim is active and corresponding payment type is enabled.
    ///     Therefore, when true is provided, only active recurrent payments will be returned;
    ///     when false only inactive; when null activity will not be taken into account.</param>
    /// <param name="pageSize">How many records to return. Maximum allowed value is 10000.</param>
    /// <returns>A collection of discovered payments. When collection is empty, there is no payments for the provided search criteria.</returns>
    Task<IReadOnlyList<RecurrentPayment>> FindRecurrentPaymentsAsync(
        int? afterId = null,
        bool? activityStatus = true,
        int pageSize = 100);

    /// <summary>
    /// Searches for finance operations of a recurrent payment using the key-set pagination.
    /// </summary>
    /// <param name="recurrentPaymentId">Recurrent payment to to search finance operations of.</param>
    /// <param name="forPeriod">UTC date for period to search finance operations of.</param>
    /// <param name="ofStates">One or more states to search finance operations of.</param>
    /// <param name="afterId">Takes finance operations with primary keys greater than provided value.</param>
    /// <param name="pageSize">How many records to return. Maximum allowed value is 10000.</param>
    /// <returns>A collection of discovered finance operations. When collection is empty, there is no finance operations for the provided search criteria.</returns>
    Task<IReadOnlyList<FinanceOperation>> FindOperationsOfRecurrentPaymentAsync(
        int recurrentPaymentId,
        DateTime? forPeriod = null,
        IReadOnlySet<FinanceOperationState>? ofStates = null,
        int? afterId = null,
        int pageSize = 100);
}
