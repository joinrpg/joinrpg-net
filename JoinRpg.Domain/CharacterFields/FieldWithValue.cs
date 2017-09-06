using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JetBrains.Annotations;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Helpers;

// ReSharper disable once CheckNamespace
namespace JoinRpg.Domain
{
  public class FieldWithValue 
  {
    private string _value;

    private IReadOnlyList<int> SelectedIds { get; set; }
    public FieldWithValue(ProjectField field, [CanBeNull] string value)
    {
      Field = field;
      Value = value;
    }

    public ProjectField Field { get; }

    [CanBeNull]
    public string Value
    {
      get { return _value; }
      set
      {
        _value = value;
        if (Field.HasValueList())
        {
          SelectedIds = Value.ToIntList();
        }
      }
    }

    [NotNull]
    public string DisplayString => GetDisplayValue(Value, SelectedIds);

    protected string GetDisplayValue(string value, IReadOnlyList<int> selectedIDs)
    {
      if (!Field.HasValueList())
      {
        return value ?? "";
      }
      return
        Field.DropdownValues.Where(dv => selectedIDs.Contains(dv.ProjectFieldDropdownValueId))
          .Select(dv => dv.Label)
          .JoinStrings(", ");
    }

    public bool HasEditableValue => !string.IsNullOrWhiteSpace(Value);

    public bool HasViewableValue => !string.IsNullOrWhiteSpace(Value) || !Field.CanHaveValue();

    public IEnumerable<ProjectFieldDropdownValue> GetPossibleValues()
    {
      return Field.GetOrderedValues().Where(v => v.IsActive || SelectedIds.Contains(v.ProjectFieldDropdownValueId));
    }

    [ItemNotNull, NotNull]
    public IEnumerable<ProjectFieldDropdownValue> GetDropdownValues()
    {
      return Field.GetOrderedValues().Where(v => SelectedIds.Contains(v.ProjectFieldDropdownValueId));
    }

    [NotNull, ItemNotNull]
    public IEnumerable<CharacterGroup> GetSpecialGroupsToApply()
    {
      return Field.HasSpecialGroup() ? GetDropdownValues().Select(c => c.CharacterGroup) : Enumerable.Empty<CharacterGroup>();
    }

    public bool HasViewAccess(AccessArguments accessArguments)
    {
      return Field.IsPublic
        || accessArguments.MasterAccess
        ||
        (accessArguments.PlayerAccessToCharacter && Field.CanPlayerView &&
         Field.FieldBoundTo == FieldBoundTo.Character)
        ||
        (accessArguments.PlayerAccesToClaim && Field.CanPlayerView &&
         Field.FieldBoundTo == FieldBoundTo.Claim);
    }

    public bool HasEditAccess(AccessArguments accessArguments, IClaimSource target)
    {
      return (accessArguments.MasterAccess
             ||
             (accessArguments.PlayerAccessToCharacter && Field.CanPlayerEdit &&
              Field.FieldBoundTo == FieldBoundTo.Character)
             ||
             (accessArguments.PlayerAccesToClaim && Field.CanPlayerEdit &&
             (Field.ShowOnUnApprovedClaims || accessArguments.PlayerAccessToCharacter)));
    }

    public override string ToString() => $"{Field.FieldName}={Value}";

        /// <summary>
        /// Returns value as integer with respect to field type.
        /// If current value could not be converted, returns default(int)
        /// </summary>
        public int ToInt()
        {
            int result;
            if (!int.TryParse(Value, out result))
                result = default(int);
            return result;
        }

    public const string CheckboxValueOn = "on";
  }
}
