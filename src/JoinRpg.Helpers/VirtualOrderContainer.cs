using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace JoinRpg.Helpers
{
    public interface IOrderableEntity
    {
        int Id { get; }
    }

    public static class VirtualOrderContainerFacade
    {
        //Factory function enable type inference
        public static VirtualOrderContainer<TChild> Create<TChild>(IEnumerable<TChild> childs,
            string ordering) where TChild : class, IOrderableEntity => new VirtualOrderContainer<TChild>(ordering, childs);
    }

    public class VirtualOrderContainer<TItem> where TItem : class, IOrderableEntity
    {
        private const char Separator = ',';


        [NotNull, ItemNotNull]
        private List<TItem> Items { get; } = new List<TItem>();


        public VirtualOrderContainer([CanBeNull]
            string storedOrder,
            [ItemNotNull] [NotNull]
            IEnumerable<TItem> entites)
        {
            storedOrder ??= "";
            if (entites == null)
            {
                throw new ArgumentNullException(nameof(entites));
            }

            var list = entites.ToList(); // Copy 

            foreach (var virtualOrderItem in ParseStoredData(storedOrder))
            {
                var item = FindItem(list, virtualOrderItem);
                if (item != null)
                {
                    Items.Add(item);
                }
            }

            foreach (var item in list.OrderBy(li => li.Id))
            {
                Items.Add(item);
            }
        }

        private IEnumerable<int> ParseStoredData(string storedOrder)
        {
            return storedOrder.Split(Separator).WhereNotNullOrWhiteSpace()
                .Select(orderItem => int.Parse(orderItem.Trim()));
        }

        private static TItem FindItem(ICollection<TItem> list, int virtualOrderItem)
        {
            var item = list.FirstOrDefault(i => i.Id == virtualOrderItem);
            if (item != null)
            {
                list.Remove(item);
            }

            return item;
        }

        public string GetStoredOrder() => string.Join(Separator.ToString(), Items.Select(item => item.Id));

        public IReadOnlyList<TItem> OrderedItems => Items.AsReadOnly();

        public void MoveDown(TItem item) => Move(item, 1);

        public void MoveUp(TItem item) => Move(item, -1);

        private void MoveToIndex(int fromIndex, int targetIndex)
        {
            if (targetIndex > Items.Count || targetIndex < 0)
            {
                throw new InvalidOperationException("Can't move item beyond the edges");
            }

            if (targetIndex == Items.Count && fromIndex == Items.Count)
            {
                throw new InvalidOperationException("Can't move item beyond the edges");
            }

            var movingItem = Items[fromIndex];
            if (fromIndex > targetIndex)
            {
                for (var i = fromIndex; i > targetIndex; i--)
                {
                    Items[i] = Items[i - 1];
                }
            }
            else if (targetIndex > fromIndex)
            {
                for (var i = fromIndex; i < targetIndex; i++)
                {
                    Items[i] = Items[i + 1];
                }
            }

            Items[targetIndex] = movingItem;
        }

        private int GetIndex(TItem field)
        {
            var index = Items.IndexOf(field);
            if (index == -1)
            {
                throw new ArgumentException("Item not exists in list", nameof(field));
            }

            return index;
        }

        public VirtualOrderContainer<TItem> Move(TItem field, short direction)
        {
            if (direction != -1 && direction != 1)
            {
                throw new ArgumentException(nameof(direction));
            }

            var index = GetIndex(field);
            var targetIndex = index + direction;
            if (targetIndex == Items.Count)
            {
                throw new InvalidOperationException("Can't move item beyond the edges");
            }

            MoveToIndex(index, targetIndex);
            return this;
        }

        public VirtualOrderContainer<TItem> MoveAfter(TItem field, TItem afterItem)
        {
            var index = GetIndex(field);
            if (afterItem == null)
            {
                MoveToIndex(index, 0);
            }
            else
            {
                var targetIndex = GetIndex(afterItem) + 1;
                if (targetIndex > index)
                {
                    targetIndex--;
                }

                MoveToIndex(index, targetIndex);
            }

            return this;
        }
    }
}
