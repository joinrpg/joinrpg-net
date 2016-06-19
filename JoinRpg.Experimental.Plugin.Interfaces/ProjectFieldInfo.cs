using System.Collections.Generic;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  public class ProjectFieldInfo
  {
    public ProjectFieldInfo(int fieldId, string fieldName, IDictionary<int, string> values)
    {
      FieldId = fieldId;
      FieldName = fieldName;
      Values = values;
    }

    public int FieldId { get; }
    public string FieldName { get; }

    public IDictionary<int, string> Values { get; }

    public override string ToString() => $"{FieldName}";
  }
}