// ReSharper disable IdentifierTypo
using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Statuses of refund operations
/// </summary>
[JsonConverter(typeof(IdentifiableEnumConverter))]
public enum RefundStatus
{
    /// <summary>
    /// Refund is in processing
    /// </summary>
    [Identifier("PROC")]
    Processing,

    /// <summary>
    /// Refund successfully completed
    /// </summary>
    [Identifier("END")]
    Completed,

    /// <summary>
    /// Error during refund processing
    /// </summary>
    [Identifier("ERR")]
    Error,
}
