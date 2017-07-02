using JoinRpg.DataModel;

namespace JoinRpg.Domain.CharacterFields
{
  public interface IFieldDefaultValueGenerator
  {
    string CreateDefaultValue(Claim claim, ProjectField feld);
    string CreateDefaultValue(Character claim, ProjectField field);
  }
}
