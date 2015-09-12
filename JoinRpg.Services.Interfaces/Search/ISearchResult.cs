using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace JoinRpg.Services.Interfaces.Search
{
  public enum SearchResultType
  {
    //TODO: I don't like it here. Should have UI enum for this. Split in two enums before localization.
    [Display(Name="Пользователь")]
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
