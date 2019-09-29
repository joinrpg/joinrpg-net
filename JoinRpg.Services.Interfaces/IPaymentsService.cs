using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces
{

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
        /// Comment added by payer
        /// </summary>
        public string CommentText { get; set; }
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
        /// Payment system URL to redirect to process payment
        /// </summary>
        public string RedirectUrl { get; set; }
    }

    /// <summary>
    /// Result of claim payment
    /// </summary>
    public class ClaimPaymentContext : PaymentContext { }

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
        public int PaymentId { get; set; }

    }

    /// <summary>
    /// Claim payment result 
    /// </summary>
    public class ClaimPaymentResultContext : PaymentResultContext
    {
        /// <summary>
        /// Database Id of a project
        /// </summary>
        public int ProjectId { get; set; }

        /// <summary>
        /// Database Id of a claim
        /// </summary>
        public int ClaimId { get; set; }
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
        Task<ClaimPaymentContext> InitiateClaimPayment(ClaimPaymentRequest request);

        /// <summary>
        /// Sets payment result and returns payment context
        /// </summary>
        /// <param name="paymentId">Database Id of a payment</param>
        /// <param name="succeeded">true if payment was succeeded</param>
        Task<ClaimPaymentResultContext> SetClaimPaymentResult(int paymentId, bool succeeded);
    }
}
