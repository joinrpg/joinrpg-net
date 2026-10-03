namespace JoinRpg.DomainTypes.Accommodation;

public static class RoomCategoryPlanExtensions
{
    /// <summary>
    /// Индекс «заявка → комната» по всем планам проекта: для экранов, которым нужна комната сразу
    /// у многих заявок (печать конвертов на весь проект).
    /// </summary>
    /// <remarks>
    /// Поштучный <see cref="RoomCategoryPlan.FindRoomByClaim"/> отвечает за O(1), но вызывающему
    /// пришлось бы знать, по каким заявкам спрашивать; здесь набор собирается сам — одним проходом
    /// по расселённым группам планов. Что заявка попадёт максимум в одну комнату, гарантирует
    /// инвариант конструктора плана: заявка входит не более чем в одну группу.
    /// </remarks>
    public static IReadOnlyDictionary<ClaimIdentification, RoomInfo> BuildRoomByClaimIndex(
        this IEnumerable<RoomCategoryPlan> plans)
    {
        ArgumentNullException.ThrowIfNull(plans);

        var index = new Dictionary<ClaimIdentification, RoomInfo>();

        foreach (var plan in plans)
        {
            foreach (var group in plan.Groups)
            {
                if (group.RoomId is not { } roomId)
                {
                    continue;
                }

                var room = plan.GetRoom(roomId);
                foreach (var claimId in group.Subjects)
                {
                    index[claimId] = room;
                }
            }
        }

        return index;
    }
}
