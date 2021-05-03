namespace PscbApi
{
    /// <summary>
    /// Interface for secrets provider service
    /// </summary>
    public interface IBankSecretsProvider
    {
        /// <summary>
        /// Merchant id
        /// </summary>
        string MerchantId { get; }

        /// <summary>
        /// Merchant Id for the Fast Payments System
        /// </summary>
        string MerchantIdFastPayments { get; }

        /// <summary>
        /// Api key
        /// </summary>
        string ApiKey { get; }

        /// <summary>
        /// Api key for sandbox access
        /// </summary>
        string ApiDebugKey { get; }
    }
}
