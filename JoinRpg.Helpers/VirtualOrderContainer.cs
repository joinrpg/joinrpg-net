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
    string PrefixForEntity { get; }
  }

  public class VirtualOrderConstants
  {
    //Generic classes will have a copy of this for each argument
    protected static char[] Digits { get; } = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

    protected const char Separator = ',';
  }


  public class VirtualOrderContainer<TItem> : VirtualOrderConstants where TItem : class, IOrderableEntity
  {
   
    private class VirtualOrderItem : IEquatable<VirtualOrderItem>
    {
      

      [NotNull]
      private string Prefix{ get; }
      private int Id { get; }

      public VirtualOrderItem([NotNull] IOrderableEntity item)
      {
        Prefix = item.PrefixForEntity;
        Id = item.Id;
      }

      public VirtualOrderItem(string orderItem)
      {
        var digitStart = orderItem.IndexOfAny(Digits);
        if (digitStart == -1)
        {
          throw new ArgumentException("Can't convert orderItem", nameof(orderItem));
        }
        Prefix = orderItem.TakeWhile(ch => !char.IsDigit(ch)).AsString();
        Id = int.Parse(orderItem.SkipWhile(ch => !char.IsDigit(ch)).AsString());
      }

      public bool Equals([CanBeNull] VirtualOrderItem other)
      {
        return other != null && Prefix == other.Prefix && Id == other.Id;
      }

      public override bool Equals([CanBeNull] object obj)
      {
        var other = obj as VirtualOrderItem;
        return Equals(other);
      }

      public override int GetHashCode()
      {
        return Prefix.GetHashCode() ^ Id;
      }

      public override string ToString()
      {
        return Prefix+ Id;
      }
    }

    [NotNull, ItemNotNull]
    private List<TItem>  Items { get; } = new List<TItem>();

    public VirtualOrderContainer([NotNull] string storedOrder, [ItemNotNull] [NotNull] IEnumerable<TItem> entites)
    {
      if (storedOrder == null) throw new ArgumentNullException(nameof(storedOrder));
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
      foreach (var item in list.OrderBy(li => li.Id).ThenBy(li => li.PrefixForEntity))
      {
        Items.Add(item);
      }
    }

    private IEnumerable<VirtualOrderItem> ParseStoredData(string storedOrder)
    {
      return storedOrder.Split(Separator).WhereNotNullOrWhiteSpace().Select(orderItem => new VirtualOrderItem(orderItem));
    }

    private static TItem FindItem(ICollection<TItem> list, VirtualOrderItem virtualOrderItem)
    {
      var item = list.FirstOrDefault(i => virtualOrderItem.Equals(new VirtualOrderItem(i)));
      if (item!= null)
      {
        list.Remove(item);
      }
      return item;
    }

    public string GetStoredOrder()
    {
      return string.Join(Separator.ToString(), Items.Select(item => new VirtualOrderItem(item)));
    }

    public IReadOnlyList<TItem> OrderedItems => Items.AsReadOnly();

    public void MoveDown(TItem item)
    {
      MoveImpl(item, 1);
    }

    public void MoveUp(TItem item)
    {
      MoveImpl(item, -1);
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
  }
}
