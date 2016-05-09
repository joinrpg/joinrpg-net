using System;
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
    public FieldWithValue(ProjectField field, string value)
    {
      Field = field;
      Value = value;
    }

    public ProjectField Field { get; }

    public string Value { get; set; }

    [NotNull]
    public string DisplayString
    {
      get
      {
        return Field.DropdownValues.FirstOrDefault(dv => dv.ProjectFieldDropdownValueId.ToString() == Value)?.Label ??
               Value;
      }
    }

    public IEnumerable<int> GetSelectedIds()
    {
      if (!Field.HasValueList())
      {
        throw new InvalidOperationException();
      }
      return string.IsNullOrWhiteSpace(Value)
        ? Enumerable.Empty<int>()
        : Value.Split(',').WhereNotNullOrWhiteSpace().Select(int.Parse);
    }

    public bool HasValue => !string.IsNullOrWhiteSpace(Value) || Field.FieldType == ProjectFieldType.Header;
  }
}
