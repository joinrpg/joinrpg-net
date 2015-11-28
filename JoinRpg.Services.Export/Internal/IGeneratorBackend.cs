using System.Collections.Generic;

namespace JoinRpg.Services.Export.Internal
{
  internal interface IGeneratorBackend
  {
    string ContentType { get; }
    string FileExtension { get; }
    void WriteRow(IEnumerable<Cell> cells);
    byte[] Generate();
  }
}