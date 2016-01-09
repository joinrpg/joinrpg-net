using JoinRpg.DataModel;

//TODO: Actually, webby logic should be in viewModel and reside in JoinRpg.Web.Models. Move.
// ReSharper disable once CheckNamespace
namespace JoinRpg.Domain
{
  public class CharacterFieldValue
  {
    public const string HtmlIdPrefix = "field_";

    public CharacterFieldValue(ProjectCharacterField field, string value)
    {
      Field = field;
      Value = value;
    }

    public ProjectCharacterField Field { get; }

    public string Value { get; set; }

    //TODO: Should be at ViewModel
    public string FieldClientId => $"{HtmlIdPrefix}{Field.ProjectCharacterFieldId}";

    public bool HasValue => string.IsNullOrWhiteSpace(Value);

    public bool IsPresent => Field.IsActive || HasValue;
  }
}
