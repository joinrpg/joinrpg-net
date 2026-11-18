namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// A payer of the list that was not found in JoinRpg (ADR025)
/// </summary>
/// <remarks>
/// Neither the identifier the bank returned nor the payer's email matched a user, so the payments
/// of this entry carry no link to a profile.
/// </remarks>
public class PscbUnresolvedUser
{
    /// <summary>
    /// Payer identifier as the bank returned it. For JoinRpg this is meant to be the user's
    /// database id, but it is kept as text: it may be missing or not a number at all
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Payer's email as the bank returned it
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// How many payments of the list belong to this payer
    /// </summary>
    public int PaymentCount { get; set; }
}
