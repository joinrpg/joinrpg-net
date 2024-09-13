using JoinRpg.DataModel;

namespace JoinRpg.Domain.CharacterFields;

public interface IFieldDefaultValueGenerator
{
    string? CreateDefaultValue(Claim? claim, FieldWithValue field);
    string? CreateDefaultValue(Character? character, FieldWithValue field);
}
