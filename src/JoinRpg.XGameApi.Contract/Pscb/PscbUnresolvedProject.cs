namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// A payment purpose that could not be linked to a project (ADR025)
/// </summary>
/// <remarks>
/// The link is missing either because nothing was found by that name or because several projects
/// share it and a choice between them would be arbitrary. Purposes that name no project at all —
/// a recurrent subscription, for one — land here as well.
/// </remarks>
public class PscbUnresolvedProject
{
    /// <summary>
    /// Project name as it was taken from the payment purpose
    /// </summary>
    public string? ProjectName { get; set; }

    /// <summary>
    /// How many payments of the list carry this name
    /// </summary>
    public int PaymentCount { get; set; }
}
