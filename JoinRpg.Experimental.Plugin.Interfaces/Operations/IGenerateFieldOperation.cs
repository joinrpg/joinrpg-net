namespace JoinRpg.Experimental.Plugin.Interfaces
{
  public interface IGenerateFieldOperation : IPluginOperation
  {
    string GenerateFieldValue(CharacterFieldInfo fieldInfo);
  }
}
