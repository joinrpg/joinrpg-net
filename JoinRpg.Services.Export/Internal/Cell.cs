using JetBrains.Annotations;

namespace JoinRpg.Services.Export.Internal
{
  internal class Cell
  {
    [CanBeNull]
    public object Content { get; set; }

    public bool ColumnHeader { get; set; }
  }
}