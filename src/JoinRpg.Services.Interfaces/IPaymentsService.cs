using JoinRpg.DataModel;
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
    public string CommentText { get; set; }

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
    /// true to initiate recurrent payment
    /// </summary>
    public bool Recurrent { get; set; }
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
    /// Payment request description built by bank API
    /// </summary>
    public PaymentRequestDescriptor RequestDescriptor { get; set; }
}

/// <summary>
/// Result of claim payment
/// </summary>
public class ClaimPaymentContext : PaymentContext
{
}

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
    /// Updates status of a proposed payment
    /// </summary>
    /// <param name="projectId">Database Id of a project</param>
    /// <param name="claimId">Database Id of a claim</param>
    /// <param name="orderId">Finance operation Id</param>
    Task UpdateClaimPaymentAsync(int projectId, int claimId, int orderId);

    /// <summary>
    /// Updates status of the last proposed payment
    /// </summary>
    /// <param name="projectId">Database Id of a project</param>
    /// <param name="claimId">Database Id of a claim</param>
    Task UpdateLastClaimPaymentAsync(int projectId, int claimId);

    /// <summary>
    /// Sets recurrent payment to cancelled internally and tries to cancel on the bank side.
    /// </summary>
    /// <param name="projectId">Id of a project</param>
    /// <param name="claimId">Id of a claim</param>
    /// <param name="paymentId">Id of a recurrent payment</param>
    /// <returns>true when payment was successfully cancelled, false otherwise, null when it was not even possible to start</returns>
    Task<bool?> CancelRecurrentPaymentAsync(int projectId, int claimId, int paymentId);

    /// <summary>
    /// Tries to charge <paramref name="amount"/> money using payment method assigned with a specified recurrent payment.
    /// </summary>
    /// <param name="projectId">Id of a project</param>
    /// <param name="claimId">Id of a claim</param>
    /// <param name="paymentId">Id of a recurrent payment</param>
    /// <param name="amount">How much money to charge</param>
    /// <returns>An instance of the <see cref="FinanceOperation"/> or null when it was even not possible to initiate operation.</returns>
    /// <remarks>
    /// Payments are typically asynchronous operations. It is necessary to update its state
    /// a little bit later using <see cref="UpdateClaimPaymentAsync"/>.
    /// </remarks>
    Task<FinanceOperation?> PerformRecurrentPayment(int projectId, int claimId, int paymentId, int amount);
}
