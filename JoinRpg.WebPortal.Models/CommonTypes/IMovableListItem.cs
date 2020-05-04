using System.Collections.Generic;
using System.Linq;

namespace JoinRpg.Web.Models.CommonTypes
{
    public interface IMovableListItem
    {
        bool First { get; set; }
        bool Last { get; set; }
        int ProjectId { get; }
        int ItemId { get; }
    }

    public static class MovableListItemExtensions
    {
        public static IList<T> MarkFirstAndLast<T>(this IList<T> collection) where T : IMovableListItem
        {
            if (collection.Any())
            {
                collection.First().First = true;
                collection.Last().Last = true;
            }
            return collection;
        }

        public static IList<T> MarkFirstAndLast<T>(this IEnumerable<T> collection) where T : IMovableListItem
        {
            return collection.ToArray().MarkFirstAndLast();
        }
    }
}
