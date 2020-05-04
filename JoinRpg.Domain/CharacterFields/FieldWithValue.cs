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
        private string _value;

        private IReadOnlyList<int> SelectedIds { get; set; }

        private Lazy<IReadOnlyList<ProjectFieldDropdownValue>> OrderedValueCache { get; }

        public FieldWithValue(ProjectField field, [CanBeNull] string value)
        {
            Field = field;
            Value = value;
            OrderedValueCache = new Lazy<IReadOnlyList<ProjectFieldDropdownValue>>(() => Field.GetOrderedValues());
        }

        public ProjectField Field { get; }

        [CanBeNull]
        public string Value
        {
            get => _value;
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
            if (Field.FieldType == ProjectFieldType.Checkbox)
            {
                return Value?.StartsWith(CheckboxValueOn) == true ? "☑️" : "☐";
            }

            if (Field.HasValueList())
            {
                return
                    Field.DropdownValues.Where(dv =>
                            selectedIDs.Contains(dv.ProjectFieldDropdownValueId))
                        .Select(dv => dv.Label)
                        .JoinStrings(", ");
            }

            return value ?? "";
        }

        public bool HasEditableValue => !string.IsNullOrWhiteSpace(Value);

        public bool HasViewableValue => !string.IsNullOrWhiteSpace(Value) || !Field.CanHaveValue();

        public IEnumerable<ProjectFieldDropdownValue> GetPossibleValues(
            AccessArguments modelAccessArguments)
        {
            return OrderedValueCache.Value.Where(v =>
                SelectedIds.Contains(v.ProjectFieldDropdownValueId) ||
                (v.IsActive && (v.PlayerSelectable || modelAccessArguments.MasterAccess))
                );
        }

        [ItemNotNull, NotNull]
        public IEnumerable<ProjectFieldDropdownValue> GetDropdownValues() => OrderedValueCache.Value.Where(v => SelectedIds.Contains(v.ProjectFieldDropdownValueId));

        [NotNull, ItemNotNull]
        public IEnumerable<CharacterGroup> GetSpecialGroupsToApply() => Field.HasSpecialGroup() ? GetDropdownValues().Select(c => c.CharacterGroup) : Enumerable.Empty<CharacterGroup>();

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

        public bool HasEditAccess(AccessArguments accessArguments)
        {
            return accessArguments.MasterAccess
                   ||
                   (accessArguments.PlayerAccessToCharacter && Field.CanPlayerEdit &&
                    Field.FieldBoundTo == FieldBoundTo.Character)
                   ||
                   (accessArguments.PlayerAccesToClaim && Field.CanPlayerEdit &&
                   (Field.ShowOnUnApprovedClaims || accessArguments.PlayerAccessToCharacter));
        }

        public override string ToString() => $"{Field.FieldName}={Value}";

        /// <summary>
        /// Returns value as integer with respect to field type.
        /// If current value could not be converted, returns default(int)
        /// </summary>
        public int ToInt()
        {
            if (!int.TryParse(Value, out var result))
            {
                result = default(int);
            }

            return result;
        }

        public const string CheckboxValueOn = "on";

        public bool SupportsPricing()
        {
            return Field.FieldType.SupportsPricing() &&
                   ((Field.FieldType.SupportsPricingOnField() && Field.Price != 0)
                    || (!Field.FieldType.SupportsPricingOnField() &&
                        Field.DropdownValues.Any(v => v.Price != 0)));
        }

        /// <summary>
        /// Special field - character name
        /// </summary>
        public bool IsName => Field.IsName();

        /// <summary>
        /// Special field - character description
        /// </summary>
        public bool IsDescription => Field.IsDescription();
        /// <summary>
        /// Special field - schedule time slot
        /// </summary>
        public bool IsTimeSlot => Field.IsTimeSlot();
        /// <summary>
        /// Special field - schedule room slot
        /// </summary>
        public bool IsRoomSlot => Field.IsRoomSlot();
    }
}
