using System.Collections.Generic;
using System.Linq;

namespace JoinRpg.Helpers
{
    public static class EntityFrameworkExtensions
    {
        /// <summary>
        /// This is required by LINQ to correctly change links list. Simple <code>entity.TargetList = newValue</code> will not work (LINQ will add NEW links).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="newValue"></param>
        public static void AssignLinksList<T>(this ICollection<T> target, ICollection<T> newValue)
        {
            foreach (var parent in target.Except(newValue).ToList())
            {
                target.Remove(parent);
            }

            target.AddLinkList(newValue);
        }

        public static void AddLinkList<T>(this ICollection<T> target, IEnumerable<T> newValue)
        {
            foreach (var parent in newValue.Except(target).ToList())
            {
                target.Add(parent);
            }
        }

        public static void CleanLinksList<T>(this ICollection<T> target)
        {
            target.AssignLinksList(new List<T>());
        }

        public static void RemoveFromLinkList<T>(this ICollection<T> target,
            IEnumerable<T> linksToRemove)
        {
            foreach (var value in linksToRemove.Intersect(target).ToList())
            {
                target.Remove(value);
            }
        }
    }
}
