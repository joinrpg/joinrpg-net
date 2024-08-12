namespace PscbApi;

/// <summary>
/// Interface for secrets provider service
/// </summary>
public interface IBankSecretsProvider
{
    /// <summary>
    /// true to use debug endpoint and credentials
    /// </summary>
    bool Debug { get; }

    bool DebugOutput { get; }

    /// <summary>
    /// Api Endpoint
    /// </summary>
    string ApiEndpoint { get; }

    /// <summary>
    /// Api Endpoint to be used in debug mode
    /// </summary>
    string ApiDebugEndpoint { get; }

    /// <summary>
    /// Merchant id
    /// </summary>
    string MerchantId { get; }

    /// <summary>
    /// Api key
    /// </summary>
    string ApiKey { get; }

    /// <summary>
    /// Api key for sandbox access
    /// </summary>
    string ApiDebugKey { get; }

    /// <summary>
    /// Url template in the bank payments system to the page with payment details.
    /// Formatted using the string.Format, bank's payment Id is at position {0}
    /// </summary>
    string BankSystemPaymentUrl { get; }

    /// <summary>
    /// Url template in the bank payments system sandbox to the page with payment details.
    /// Formatted using the string.Format, bank's payment Id is at position {0}
    /// </summary>
    string BankSystemDebugPaymentUrl { get; }
}
