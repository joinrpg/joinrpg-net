using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Selection mode of the payments list request: which payment statuses to include
/// </summary>
[JsonConverter(typeof(IdentifiableEnumConverter))]
public enum PaymentsSelectMode
{
    /// <summary>
    /// Only completed payments. This is the bank's default when the parameter is omitted
    /// </summary>
    [Identifier("paid")]
    Paid,

    /// <summary>
    /// All payments, including the unfinished ones
    /// </summary>
    [Identifier("created")]
    Created,
}
