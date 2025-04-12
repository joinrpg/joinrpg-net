namespace JoinRpg.Helpers;

public static class VirtualOrderSorterExtensions
{
    public static IOrderedEnumerable<TElement> OrderByStoredOrder<TElement>(this IEnumerable<TElement> elements, string? storedOrder, bool preserveOrder = false)
        where TElement : IOrderableEntity
    {
        return elements.OrderByStoredOrder(e => e.Id, storedOrder, preserveOrder);
    }

    public static IOrderedEnumerable<TElement> OrderByStoredOrder<TElement>(
        this IEnumerable<TElement> elements,
        Func<TElement, int> keySelector,
        string? storedOrder,
        bool preserveOrder = false)
    {
        return elements.OrderBy(keySelector, new ByStoredOrderComparer(storedOrder, preserveOrder));
    }

    private class ByStoredOrderComparer(string? orderStr, bool preserveOrder = false) : IComparer<int>
    {
        private readonly List<int> order = orderStr.AsSpan().ParseToIntList();
        public int Compare(int x, int y)
        {
            var xIdx = order.IndexOf(x);
            var yIdx = order.IndexOf(y);
            return (xIdx, yIdx) switch
            {
                (-1, -1) =>
                    preserveOrder
                        ? 0  // Нет порядка, сохраняем старый
                        : Comparer<int>.Default.Compare(x, y), //Нет порядка, идем по Id
                (-1, _) => -1, // Y не в порядке, а Х — в нем — значит Х раньше
                (_, -1) => 1, // X не в порядке, а Y — в нем — значит Y раньше
                (_, _) => Comparer<int>.Default.Compare(xIdx, yIdx), // Порядок есть, используем его 
            };
        }
    }
}
