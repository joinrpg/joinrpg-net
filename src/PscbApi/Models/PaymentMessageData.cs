// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Additional data for <see cref="PaymentMessage"/>
/// </summary>
public class PaymentMessageData
{
    /// <summary>
    /// true if payment has to be set on hold
    /// </summary>
    /// <remarks>
    /// See https://docs.pscb.ru/oos/advanced.html#dopolnitelnye-opcii-holdirovanie for details
    /// </remarks>
    [JsonProperty("hold", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool Hold { get; set; }

    /// <summary>
    /// Receipt data to be sent to Fiscal Data Operator
    /// </summary>
    [Required]
    [JsonProperty("fdReceipt")]
    public required Receipt Receipt { get; set; }

    /// <summary>
    /// Payment page design template identifier
    /// </summary>
    /// <remarks>
    /// See https://docs.pscb.ru/oos/advanced.html#dopolnitelnye-opcii-dizajn-i-kastomizaciya-dizajn-stranicy-oplaty for details
    /// </remarks>
    [JsonProperty("template", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? TemplateName { get; set; }

    /*
    /// <summary>
    /// Qiwi Wallet login (phone number in international format). Used only for <see cref="PscbPaymentMethod.Qiwi"/> payments
    /// </summary>
    [Phone]
    [JsonProperty("user", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string QiwiWalletLogin { get; set; }

    /// <summary>
    /// Mobile phone number in international format for mobile payments. Used only for <see cref="PscbPaymentMethod.Mobile"/> payments
    /// </summary>
    [Phone]
    [JsonProperty("userPhone", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string MobilePaymentPhoneNumber { get; set; }

    /// <summary>
    /// Alfa Click user account. Used only for <see cref="PscbPaymentMethod.AlfaClick"/> payments
    /// </summary>
    [JsonProperty("userAccount", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string AlfaClickLogin { get; set; }
    */

    /// <summary>
    /// true to enable debug information in Payer' browser
    /// </summary>
    /// <remarks>
    /// Field is automatically set inside <see cref="BankApi"/>
    /// </remarks>
    [JsonProperty("debug", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool EnableDebugOutput { get; set; }

    /// <summary>
    /// Additional Id
    /// </summary>
    /// <remarks>
    /// Field is automatically set inside <see cref="BankApi"/>
    /// </remarks>
    [JsonProperty("qrcId", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? AdditionalToken { get; set; }

    /// <summary>
    /// Fast Payments System recurrent payment purpose
    /// </summary>
    [JsonProperty("sbpSubscriptionPurpose", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? FastPaymentsSystemSubscriptionPurpose { get; set; }

    /// <summary>
    /// Url to return user from the Fast Payments System back here
    /// </summary>
    [JsonProperty("sbpRedirectUrl", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? FastPaymentsSystemRedirectUrl { get; set; }
}

/// <summary>
/// Provides additional properties to support Fast Payments System invoicing
/// </summary>
/// <remarks>
/// See https://docs.pscb.ru/oos/api.html#api-dopolnitelnyh-vozmozhnostej-invojsing
/// </remarks>
public class FastPaymentSystemInvoicingMessageData : PaymentMessageData
{
    /// <summary>
    /// Width of QR-code image. Only for FPS invoicing
    /// </summary>
    [JsonProperty("qrCodeImageWidth", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public int? QrCodeWidth { get; set; }

    /// <summary>
    /// Height of QR-code image. Only for FPS invoicing
    /// </summary>
    [JsonProperty("qrCodeImageHeight", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public int? QrCodeHeight { get; set; }

    /// <summary>
    /// true to return QR code image. Only for FPS invoicing
    /// </summary>
    [JsonProperty("requestQrCodeImage", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool? GetQrCode { get; set; }

    /// <summary>
    /// true to return QR code url. Only for FPS invoicing
    /// </summary>
    [JsonProperty("requestQrCodeImageUrl", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool? GetQrCodeUrl { get; set; }
}
