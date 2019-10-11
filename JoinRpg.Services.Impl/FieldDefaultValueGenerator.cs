using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.PluginHost.Interfaces;

namespace JoinRpg.Services.Impl
{
    [UsedImplicitly]
    internal class FieldDefaultValueGenerator : IFieldDefaultValueGenerator
    {
        public FieldDefaultValueGenerator(IPluginFactory pluginFactory)
        {
            PluginFactory = pluginFactory;
        }

        private IPluginFactory PluginFactory { get; }
        public string CreateDefaultValue(Claim claim, FieldWithValue field) => null;

        public string CreateDefaultValue(Character character, FieldWithValue field)
        {
            if (field.IsName && character != null)
            {
                return character.CharacterName;
                // It helps battle akward situations where names was re-bound to some new field
                // and empty values start overwriting names
            }
            return character != null
              ? PluginFactory
                .GenerateDefaultCharacterFieldValue(character, field.Field)
              : null;
        }
    }
}
