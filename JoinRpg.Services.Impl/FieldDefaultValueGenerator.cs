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

    public string CreateDefaultValue(Character character, ProjectField field) => PluginFactory
      .GenerateDefaultCharacterFieldValue(character, field);
  }
}
