using JetBrains.Annotations;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Export.Internal
{
  internal class Cell
  {
    [CanBeNull]
    public string Content { get; set; }

    public bool ColumnHeader { get; set; }

    public CellType CellType { get; set; } = CellType.Regular;
  }
}