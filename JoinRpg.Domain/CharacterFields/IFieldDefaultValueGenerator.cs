using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.CharacterFields
{
  public interface IFieldDefaultValueGenerator
  {
    [CanBeNull]
    string CreateDefaultValue([CanBeNull] Claim claim, [NotNull] FieldWithValue field);
    [CanBeNull]
    string CreateDefaultValue([CanBeNull] Character character, [NotNull] FieldWithValue field);
  }
}
