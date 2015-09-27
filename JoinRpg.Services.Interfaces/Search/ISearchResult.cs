using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces.Search
{
  public interface ISearchResult : ILinkable
  {
    [NotNull]
    string Name { get; }
    [NotNull]
    string Description { get; }
  }
}
