using JoinRpg.DataModel;

//TODO: Actually, this should be viewModel and reside in JoinRpg.Web.Models. Move.
// ReSharper disable once CheckNamespace
namespace JoinRpg.Domain
{
  public class CharacterFieldValue
  {
    private string _value;
    public const string HtmlIdPrefix = "field_";

    public CharacterFieldValue(CharacterFieldsContainter containter, ProjectCharacterField field, string value)
    {
      Containter = containter;
      Field = field;
      _value = value;
    }

    public ProjectCharacterField Field { get; }

    public string Value
    {
      get { return _value; }
      set
      {
        _value = value;
        //TODO: We really don't need to do this on each update. I like my idea of this container, but we can do much simpler if stick with simple parse/serialize approach
        Containter.Update();
      }
    }

    //TODO: This is example of web logic we like to have here, so clearly viewModel
    public string FieldClientId => $"{HtmlIdPrefix}{Field.ProjectCharacterFieldId}";

  private CharacterFieldsContainter Containter { get; }
  }
}
