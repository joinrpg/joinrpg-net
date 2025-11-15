namespace JoinRpg.Web.Claims.UnifiedGrid;
public enum UgStatusFilterView
{
    /// <summary>
    /// Активные персонажи + активные заявки на них
    /// </summary>
    Active,
    /// <summary>
    /// Активные персонажи без утвержденных заявок + активные заявки на них
    /// </summary>
    Vacant,
    /// <summary>
    /// Активные непринятые заявки + персонажи к ним
    /// </summary>
    Discussion,
    /// <summary>
    /// (Удаленные персонажи + все заявки на них) + (активные персонажи с отклоненными и отозванными заявками + отклоненные/отозванные заявки)
    /// </summary>
    Archive,
}
