using JetBrains.Annotations;

namespace JoinRpg.Services.Interfaces.Search
{
  public enum SearchResultType
  {
    ResultUser,
    ResultCharacterGroup,
    ResultCharacter
  }

  public interface ISearchResult
  {
    SearchResultType Type { get; }
    [NotNull]
    string Name { get; }
    [NotNull]
    string Description { get; }

    [CanBeNull]
    string Identification { get; }
    int? ProjectId { get; }
  }
}
