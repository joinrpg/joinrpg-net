// ReSharper disable IdentifierTypo
namespace PscbApi.Models;

/// <summary>
/// Payments system error codes
/// </summary>
public enum PaymentsSystemResponseCode
{
    /// <summary>
    /// All ok
    /// </summary>
    Success = 0,

    /// <summary>
    /// Payment is in processing
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Payment is waiting for 3D Secure password from user
    /// </summary>
    Awaiting3DSecurePassword = 2,

    /// <summary>
    /// Recurrent payment has to be confirmed by sending a sum that was blocked on the clients' side
    /// </summary>
    AwaitingRecurrentPaymentConfirmation = 3,

    /// <summary>
    /// Transaction was declined by the processing center
    /// </summary>
    DeclinedByProcessing = -1,

    /// <summary>
    /// Transaction was declined by the payments system
    /// </summary>
    DeclinedBySystem = -2,

    /// <summary>
    /// Invalid payments parameters
    /// </summary>
    InvalidPaymentParameters = -3,

    /// <summary>
    /// Card is not bind to the web wallet
    /// </summary>
    CardNotBind = -4,

    /// <summary>
    /// Transaction was declined due to unknown error
    /// </summary>
    UnknownError = -5,

    /// <summary>
    /// Invalid web wallet SMS confirmation
    /// </summary>
    InvalidWebWalletConfirmation = -14,

    /// <summary>
    /// Recurrent payments are not supported
    /// </summary>
    RecurrentPaymentsNotSupported = -15,

    /// <summary>
    /// Invalid recurrent payment parameters
    /// </summary>
    InvalidRecurrentPaymentParameters = -16,

    /// <summary>
    /// Invalid signature
    /// </summary>
    InvalidSignature = -17,

    /// <summary>
    /// Payment system limits exceeded
    /// </summary>
    LimitsExceeded = -18,

    /// <summary>
    /// Frod suspicion
    /// </summary>
    FrodSuspicion = -19
}
