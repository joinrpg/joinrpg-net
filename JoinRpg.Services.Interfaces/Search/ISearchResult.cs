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
    /// <summary>
    /// Used to indicate that a perfect match was found and other search results are odd and useless. 
    /// </summary>
    bool IsPerfectMatch { get; }
  }
}
