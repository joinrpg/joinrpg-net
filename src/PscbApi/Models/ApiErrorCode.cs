using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// PSCB API error codes
/// </summary>
[JsonConverter(typeof(IdentifiableEnumConverter))]
public enum ApiErrorCode
{
    /// <summary>
    /// Not authorized request
    /// </summary>
    [Identifier("NOT_AUTHORIZED")]
    NotAuthorized,

    /// <summary>
    /// Payment was not found
    /// </summary>
    [Identifier("UNKNOWN_PAYMENT")]
    UnknownPayment,

    /// <summary>
    /// Request is not correct
    /// </summary>
    [Identifier("ILLEGAL_REQUEST")]
    IllegalRequest,

    /// <summary>
    /// Request arguments list is not correct
    /// </summary>
    [Identifier("ILLEGAL_ARGUMENTS")]
    IllegalArguments,

    /// <summary>
    /// Requested action could not be executed
    /// </summary>
    [Identifier("ILLEGAL_ACTION")]
    IllegalAction,

    /// <summary>
    /// Not possible to perform action with current payment state
    /// </summary>
    [Identifier("ILLEGAL_PAYMENT_STATE")]
    IllegalPaymentState,

    /// <summary>
    /// Request result is undefined, request could be repeated
    /// </summary>
    [Identifier("REPEAT_REQUEST")]
    RepeatRequest,

    /// <summary>
    /// Operation is still processing
    /// </summary>
    [Identifier("PROCESSING")]
    Processing,

    /// <summary>
    /// Server error
    /// </summary>
    [Identifier("SERVER_ERROR")]
    ServerError,
}
