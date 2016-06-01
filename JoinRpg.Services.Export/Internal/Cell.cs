using JetBrains.Annotations;

namespace JoinRpg.Services.Export.Internal
{
  internal class Cell
  {
    [CanBeNull]
    public string Content { get; set; }

    public bool ColumnHeader { get; set; }

    public bool IsUri { get; set; }
  }
}