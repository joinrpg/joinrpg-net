using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JetBrains.Annotations;
using JoinRpg.Helpers;

// ReSharper disable once CheckNamespace
namespace JoinRpg.Domain
{
  public class FieldWithValue 
  {
    private string _value;

    private IReadOnlyList<int> SelectedIds { get; set; }
    public FieldWithValue(ProjectField field, string value)
    {
      Field = field;
      Value = value;
    }

    public ProjectField Field { get; }

    public string Value
    {
      get { return _value; }
      set
      {
        _value = value;
        if (Field.HasValueList())
        {
          SelectedIds = string.IsNullOrWhiteSpace(Value)
            ? new int[] { }
            : Value.ToIntList();
        }
      }
    }

    [NotNull]
    public string DisplayString
    {
      get
      {
        if (!Field.HasValueList())
        {
          return Value;
        }
        return
          Field.DropdownValues.Where(dv => SelectedIds.Contains(dv.ProjectFieldDropdownValueId))
            .Select(dv => dv.Label)
            .Join(", ");
      }
    }

    public bool HasValue => !string.IsNullOrWhiteSpace(Value) || Field.FieldType == ProjectFieldType.Header;

    public IEnumerable<ProjectFieldDropdownValue> GetPossibleValues()
    {
      return Field.GetOrderedValues().Where(v => v.IsActive || SelectedIds.Contains(v.ProjectFieldDropdownValueId));
    }

    [ItemNotNull, NotNull]
    public IEnumerable<ProjectFieldDropdownValue> GetDropdownValues()
    {
      return Field.GetOrderedValues().Where(v => SelectedIds.Contains(v.ProjectFieldDropdownValueId));
    }
  }
}
