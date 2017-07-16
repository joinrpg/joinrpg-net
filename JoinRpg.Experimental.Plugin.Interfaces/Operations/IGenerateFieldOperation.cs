using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  public interface IGenerateFieldOperation : IPluginOperation, IFieldOperation
  {
    string GenerateFieldValue([NotNull] CharacterInfo character, [NotNull] CharacterFieldInfo fieldInfo);
  }

  public interface IFieldOperation
  {
  }
}
