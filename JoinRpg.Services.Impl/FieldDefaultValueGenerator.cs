using JetBrains.Annotations;
using JoinRpg.DataModel;
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
        public string CreateDefaultValue(Claim claim, ProjectField feld) => null;

        public string CreateDefaultValue(Character character, ProjectField field)
        {
            if (field == field.Project.Details.CharacterNameField && character != null)
            {
                return character.CharacterName;
                // It helps battle akward situations where names was re-bound to some new field
                // and empty values start overwriting names
            }
            return character != null
              ? PluginFactory
                .GenerateDefaultCharacterFieldValue(character, field)
              : null;
        }
    }
}
