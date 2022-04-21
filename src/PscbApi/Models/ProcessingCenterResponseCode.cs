// ReSharper disable IdentifierTypo
#pragma warning disable 1591
namespace PscbApi.Models;

/// <summary>
/// Response codes from the processing center
/// </summary>
public enum ProcessingCenterResponseCode
{
    /// <summary>
    /// Service is unavailable under generic circumstances
    /// </summary>
    ServiceUnavailable = 100,

    /// <summary>
    /// Service is unavailable due to maintenance
    /// </summary>
    Maintenance = 101,

    /// <summary>
    /// Processing gate is not accessible
    /// </summary>
    ProcessingGateInaccessible = 102,

    /// <summary>
    /// User presses F5 during payment processing
    /// </summary>
    DuplicateTransaction = 103,

    /// <summary>
    /// Session failure on server
    /// </summary>
    SessionFailure = 104,

    /// <summary>
    /// Request validation failed
    /// </summary>
    ValidationFailed = 105,

    /// <summary>
    /// Phone number was not passed for service paid by web wallet
    /// </summary>
    MissedPhoneNumber = 106,

    TransactionExpired = -20,

    AuthenticationFailed = -19,

    AccessDenied = -17,

    TerminalIsLocked = -16,

    /// <summary>
    /// Card expiration date is invalid by some reason
    /// </summary>
    InvalidExpirationDate = -9,

    ServerNotResponding = -4,

    InvalidOrMissedResponse = -3,

    BadCgiRequest = -2,

    Approved = 0,

    CallYourBank = 1,

    InvalidMerchant = 3,

    CardIsRestricted = 4,

    TransactionDeclined = 5,

    RetryRequired = 6,

    InvalidTransaction = 12,

    InvalidAmount = 13,

    NoSuchCard = 14,

    NoSuchCardOrIssuer = 15,

    ReEnterTransaction = 19,

    InvalidResponse = 20,

    FormatError = 30,

    LostCard = 41,

    StolenCard = 43,

    NotSufficientFunds = 51,

    ExpiredCard = 54,

    IncorrectPin = 55,

    NotPermittedToClient = 57,

    NotPermittedToMerchant = 58,

    ExceedsAmountLimit = 61,

    RestrictedCard = 62,

    ExceedsFrequencyLimit = 65,

    PinTriesExceeded = 75,

    Reserved = 78,

    TimeOutAtIssuer = 82,

    AuthenticationFailure = 89,

    IssuerUnavailable = 91,

    LawViolation = 93,

    SystemMalfunction = 96,
}
