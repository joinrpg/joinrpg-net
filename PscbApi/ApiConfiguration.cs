namespace PscbApi
{

    /// <summary>
    /// Configuration for <see cref="BankApi"/>
    /// </summary>
    public struct ApiConfiguration
    {
        /// <summary>
        /// Set to true to use debug api endpoint. <see cref="ApiKey"/> must contain debug key
        /// </summary>
        public bool Debug;

        /// <summary>
        /// Api key (issued by PSCB)
        /// </summary>
        public string ApiKey;

        /// <summary>
        /// Api key to access debug endpoint (issued by PSCB)
        /// </summary>
        public string ApiDebugKey;

        /// <summary>
        /// Merchant Id (issued by PSCB)
        /// </summary>
        public string MerchantId;

        /// <summary>
        /// Default url for successful payments
        /// </summary>
        public string DefaultSuccessUrl;

        /// <summary>
        /// Default url for failed payments
        /// </summary>
        public string DefaultFailUrl;
    }
}
