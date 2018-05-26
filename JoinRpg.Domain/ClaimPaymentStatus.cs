namespace JoinRpg.Domain
{
    /// <summary>
    /// Describes current payment status
    /// </summary>
    public enum ClaimPaymentStatus
    {
        /// <summary>
        /// Claim is completedly paid
        /// </summary>
        Paid,

        /// <summary>
        /// There are more money to pay
        /// </summary>
        Overpaid,

        /// <summary>
        /// Claim is paid partially
        /// </summary>
        MoreToPay,

        /// <summary>
        /// Claim is not paid
        /// </summary>
        NotPaid,
    }
}
