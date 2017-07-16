using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.CharacterFields
{
  public interface IFieldDefaultValueGenerator
  {
    [CanBeNull]
    string CreateDefaultValue([CanBeNull] Claim claim, [NotNull] ProjectField feld);
    [CanBeNull]
    string CreateDefaultValue([CanBeNull] Character character, [NotNull] ProjectField field);
  }
}
