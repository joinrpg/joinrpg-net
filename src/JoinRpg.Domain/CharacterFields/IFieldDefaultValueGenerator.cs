using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.CharacterFields
{
    public interface IFieldDefaultValueGenerator
    {
        string? CreateDefaultValue([CanBeNull] Claim? claim, [NotNull] FieldWithValue field);
        string? CreateDefaultValue([CanBeNull] Character? character, [NotNull] FieldWithValue field);
    }
}
