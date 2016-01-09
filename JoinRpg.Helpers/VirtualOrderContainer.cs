using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public static VirtualOrderContainer<TChild> Create<TChild>(IEnumerable<TChild> childs, string ordering) where TChild : class, IOrderableEntity
    {
      return new VirtualOrderContainer<TChild>(ordering, childs);
    }
  }

  public class VirtualOrderContainer<TItem> where TItem : class, IOrderableEntity
  {
    private const char Separator = ',';


    [NotNull, ItemNotNull]
    private List<TItem> Items { get; } = new List<TItem>();


    public VirtualOrderContainer([CanBeNull] string storedOrder, [ItemNotNull] [NotNull] IEnumerable<TItem> entites)
    {
      storedOrder = storedOrder ?? "";
      if (entites == null) throw new ArgumentNullException(nameof(entites));

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
      return storedOrder.Split(Separator).WhereNotNullOrWhiteSpace().Select(orderItem => int.Parse(orderItem.Trim()));
    }

    private static TItem FindItem(ICollection<TItem> list, int virtualOrderItem)
    {
      var item = list.FirstOrDefault(i => i.Id == virtualOrderItem);
      if (item!= null)
      {
        list.Remove(item);
      }
      return item;
    }

    public string GetStoredOrder()
    {
      return string.Join(Separator.ToString(), Items.Select(item =>  item.Id));
    }

    public IReadOnlyList<TItem> OrderedItems => Items.AsReadOnly();

    public void MoveDown(TItem item)
    {
      Move(item, 1);
    }

    public void MoveUp(TItem item)
    {
      Move(item, -1);
    }

    private void MoveImpl(TItem item, int direction)
    {
      var index = Items.IndexOf(item);
      if (index == -1)
      {
        throw new ArgumentException("Item not exists in list", nameof(item));
      }
      var targetIndex = index + direction;
      if (targetIndex >= Items.Count || targetIndex < 0)
      {
        throw new InvalidOperationException("Can't move item beyond the edges");
      }

      var nextItem = Items[targetIndex];
      Items[targetIndex] = item;
      Items[index] = nextItem;
    }

    public ReadOnlyCollection<TItem> GetOrderedItemsWithFilter(Func<TItem, bool> predicate)
    {
      return Items.Where(predicate).ToList().AsReadOnly();
    }

    public VirtualOrderContainer<TItem> Move(TItem field, short direction)
    {
      if (direction != -1 && direction != 1)
      {
        throw new ArgumentException(nameof(direction));
      }
      MoveImpl(field, direction);
      return this;
    }
  }
}
