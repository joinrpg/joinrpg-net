// ReSharper disable IdentifierTypo
using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Payment statuses
/// </summary>
[JsonConverter(typeof(IdentifiableEnumConverter))]
public enum PaymentStatus
{
    /// <summary>
    /// Payment was created
    /// </summary>
    [Identifier("new")]
    New,

    /// <summary>
    /// Payment was created, user was redirected to the payment page
    /// </summary>
    [Identifier("sent")]
    AwaitingForPayment,

    /// <summary>
    /// Payment was successfully paid. This status persists for payments that was partially refunded
    /// </summary>
    [Identifier("end")]
    Paid,

    /// <summary>
    /// Payment was cancelled and completely refunded
    /// </summary>
    [Identifier("ref")]
    Refunded,

    /// <summary>
    /// Payment was expired or replaced with a new one
    /// </summary>
    [Identifier("exp")]
    Expired,

    /// <summary>
    /// Sum of payment was places on hold on the Payer's card
    /// </summary>
    [Identifier("hold")]
    Hold,

    /// <summary>
    /// Two-step payment was cancelled, sum of payment was released from the Payer's card
    /// </summary>
    [Identifier("canceled")]
    Cancelled,

    /// <summary>
    /// Error occured during payment process
    /// </summary>
    [Identifier("err")]
    Error,

    /// <summary>
    /// Payment was rejected by the store
    /// </summary>
    [Identifier("rej")]
    Rejected,

    /// <summary>
    /// Payment is not defined at the moment (will be defined later)
    /// </summary>
    [Identifier("undef")]
    Undefined,
}

/// <summary>
/// Payment sub-statuses
/// </summary>
[JsonConverter(typeof(IdentifiableEnumConverter))]
public enum PaymentSubStatus
{
    /// <summary>
    /// Hold status confirmed (before switching to <see cref="PaymentStatus.Paid"/>
    /// </summary>
    [Identifier("hold_confirmed")]
    HoldConfirmed,
}
