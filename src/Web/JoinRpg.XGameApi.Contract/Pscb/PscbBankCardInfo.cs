namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// Extended data of a bank card the payment was made with
/// </summary>
/// <remarks>
/// Returned by the bank only when card data was requested and the payment was made by card.
/// The bank guarantees it no earlier than three minutes after the payment reaches a final state.
/// </remarks>
public class PscbBankCardInfo
{
    /// <summary>
    /// Bank that emitted the card
    /// </summary>
    public string? Bank { get; set; }

    /// <summary>
    /// Status of the card payment in the processing center
    /// </summary>
    public string? ProcessingStatus { get; set; }

    /// <summary>
    /// Country the card was emitted in
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// ISO 3166-1 alpha-3 code of the country the card was emitted in
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Card expiration date as the bank renders it, e.g. "2204" for April 2022
    /// </summary>
    public string? ExpirationDate { get; set; }

    /// <summary>
    /// Payment system and card type, e.g. "VISA"
    /// </summary>
    public string? CardType { get; set; }

    /// <summary>
    /// Masked card number (PAN)
    /// </summary>
    public string? MaskedNumber { get; set; }

    /// <summary>
    /// Reference Retrieval Number — unique identifier of the bank transaction
    /// </summary>
    public string? ReferenceRetrievalNumber { get; set; }

    /// <summary>
    /// Email the payer has entered on the card payment form
    /// </summary>
    public string? CardholderEmail { get; set; }
}
