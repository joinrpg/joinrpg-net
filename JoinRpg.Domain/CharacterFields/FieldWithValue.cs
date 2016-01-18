using JoinRpg.DataModel;

// ReSharper disable once CheckNamespace
namespace JoinRpg.Domain
{
  public class FieldWithValue 
  {
    public FieldWithValue(ProjectField field, string value)
    {
      Field = field;
      Value = value;
    }

    public ProjectField Field { get; }

    public string Value { get; set; }
  }
}
