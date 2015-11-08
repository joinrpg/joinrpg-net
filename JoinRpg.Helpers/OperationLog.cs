using System;
using System.Collections.Generic;

namespace JoinRpg.Helpers
{
  public class OperationLog
  {
    private ICollection<string> Log { get; } = new List<string>();
    private DateTime Start { get; } = DateTime.Now;

    public IEnumerable<string> Results => Log;

    public void Info(string str) => Add(str);

    public void Error(string str) => Add(str);

    private void Add(string str) => Log.Add($"[{(DateTime.Now - Start).TotalMilliseconds}] {str}");
  }
}