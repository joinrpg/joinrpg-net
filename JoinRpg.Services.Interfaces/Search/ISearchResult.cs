using JetBrains.Annotations;

namespace JoinRpg.Services.Interfaces.Search
{
  public enum SearchResultType
  {
    ResultUser
  }

  public interface ISearchResult
  {
    SearchResultType Type { get; }
    [NotNull]
    string Name { get; }
    [NotNull]
    string Description { get; }
    [NotNull]
    string FoundValue { get; }
    [CanBeNull]
    string Identification { get; }
  }
}
