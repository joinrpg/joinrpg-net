using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces.Search
{
  public interface ISearchResult : ILinkable
  {
    [NotNull]
    string Name { get; }
    [NotNull]
    MarkdownString Description { get; }
    bool IsPublic { get; }
    bool IsActive { get; }
  }
}
