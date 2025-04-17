using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Impl;

internal static class DomainMoveHelper
{

    public static string MoveAfter<TId>(TId targetId, TId? afterId, string? storedOrder, IEnumerable<TId> idList, bool preserveOrder = false)
        where TId : IProjectEntityId
    {
        var voc = VirtualOrderContainerFacade.Create(idList.Select(x => IdWrapper.Create(x)), storedOrder, preserveOrder);

        var resultOrder = voc.MoveAfter(targetId.Id, afterId?.Id).GetStoredOrder();
        return resultOrder;
    }

    private record IdWrapper(int Id) : IOrderableEntity

    {
        public static IdWrapper Create<TId>(TId id)
            where TId : IProjectEntityId
            => new IdWrapper(id.Id);

    }

}
