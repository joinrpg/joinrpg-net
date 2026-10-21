namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// Kind of a PSCB payment record. Used as a filter of the payments list: when the filter is not
/// set, the bank returns payments and invoices together
/// </summary>
public enum PscbPaymentKind
{
    /// <summary>
    /// Regular payment
    /// </summary>
    Payment,

    /// <summary>
    /// Invoice
    /// </summary>
    Invoice,
}
