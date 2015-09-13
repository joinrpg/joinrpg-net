using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JoinRpg.Helpers
{
  public static class StaticTaskHelpers
  {
    public static Task<Task<T>>[] Interleaved<T>(this IEnumerable<Task<T>> tasks)
    {
      return Interleaved(tasks.ToList());
    }


    /// <summary>
    /// Process task as they complete, ignoring order
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>
    /// Based on http://blogs.msdn.com/b/pfxteam/archive/2012/08/02/processing-tasks-as-they-complete.aspx
    /// </remarks>
    public static Task<Task<T>>[] Interleaved<T>(this ICollection<Task<T>> tasks)
    {
      var buckets = new TaskCompletionSource<Task<T>>[tasks.Count];
      var results = new Task<Task<T>>[buckets.Length];
      for (int i = 0; i < buckets.Length; i++)
      {
        buckets[i] = new TaskCompletionSource<Task<T>>();
        results[i] = buckets[i].Task;
      }

      int nextTaskIndex = -1;
      Action<Task<T>> continuation = completed =>
      {
        var bucket = buckets[Interlocked.Increment(ref nextTaskIndex)];
        bucket.TrySetResult(completed);
      };

      foreach (var inputTask in tasks)
        inputTask.ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

      return results;
    }
  }
}
