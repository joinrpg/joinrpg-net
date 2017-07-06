namespace JoinRpg.Experimental.Plugin.Interfaces
{
  public interface IGenerateFieldOperation : IPluginOperation, IFieldOperation
  {
    string GenerateFieldValue(CharacterInfo character, CharacterFieldInfo fieldInfo);
  }

  public interface IFieldOperation
  {
  }
}
