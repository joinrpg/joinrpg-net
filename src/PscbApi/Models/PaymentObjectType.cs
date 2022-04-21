// ReSharper disable IdentifierTypo
using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Types of payment objects
/// </summary>
[JsonConverter(typeof(IdentifiableEnumConverter))]
public enum PaymentObjectType
{
    /// <summary>
    /// Unclassified object
    /// </summary>
    [Identifier("another")]
    Unclassified,

    /// <summary>
    /// Commodity, goods
    /// </summary>
    [Identifier("commodity")]
    Commodity,

    /// <summary>
    /// Job
    /// </summary>
    [Identifier("job")]
    Job,

    /// <summary>
    /// Service
    /// </summary>
    [Identifier("service")]
    Service,

    /// <summary>
    /// Lottery ticket or similar
    /// </summary>
    [Identifier("lottery")]
    Lottery,

    /// <summary>
    /// Composite object
    /// </summary>
    [Identifier("composite")]
    Composite,
}
