using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;

namespace JoinRpg.Services.Impl
{
    [UsedImplicitly]
    internal class FieldDefaultValueGenerator : IFieldDefaultValueGenerator
    {
        public string? CreateDefaultValue(Claim? claim, FieldWithValue field) => null;

        public string? CreateDefaultValue(Character? character, FieldWithValue field)
        {
            if (field.IsName && character != null)
            {
                return character.CharacterName;
                // It helps battle akward situations where names was re-bound to some new field
                // and empty values start overwriting names
            }

            if (field.Field.FieldType == ProjectFieldType.PinCode)
            {
                var random = new Random();
                return random.Next(9999).ToString("D4");
            }
            return null;
        }
    }
}
