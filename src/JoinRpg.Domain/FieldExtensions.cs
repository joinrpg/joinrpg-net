using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain;

public static class FieldExtensions
{
    public static bool HasValueList(this ProjectField field) => field.FieldType.HasValuesList();

    public static bool SupportsMassAdding(this ProjectField field) => field.FieldType.SupportsMassAdding();

    public static bool HasSpecialGroup(this ProjectField field) => field.HasValueList() && field.FieldBoundTo == FieldBoundTo.Character;

    public static string GetSpecialGroupName(this ProjectFieldDropdownValue fieldValue) => $"{fieldValue.Label}";

    public static string GetSpecialGroupName(this ProjectField field) => $"{field.FieldName}";

    public static bool IsAvailableForTarget(this ProjectFieldInfo field, IClaimSource? target)
    {
        if (field == null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        return field.IsActive
          && (field.BoundTo == FieldBoundTo.Claim || field.ValidForNpc || !(target is Character character && character.IsNpc()))
          && (!field.GroupsAvailableForIds.Any() || target.IsPartOfAnyOfGroups(field.GroupsAvailableForIds));
    }

    public static bool CanHaveValue(this ProjectField projectField) => projectField.FieldType != ProjectFieldType.Header;

    public static ProjectFieldDropdownValue? GetBoundFieldDropdownValueOrDefault(this CharacterGroup group)
    {
        return group.Project.ProjectFields.SelectMany(pf => pf.DropdownValues)
                .SingleOrDefault(pfv => pfv.CharacterGroup == group);
    }

    /// <summary>
    /// Special field - character name
    /// </summary>
    public static bool IsName(this ProjectField field) => field.Project.Details.CharacterNameField == field;

    /// <summary>
    /// Special field - character description
    /// </summary>
    public static bool IsDescription(this ProjectField field) => field.Project.Details.CharacterDescription == field;
    /// <summary>
    /// Special field - schedule time slot
    /// </summary>
    public static bool IsTimeSlot(this ProjectField field) => field.FieldType == ProjectFieldType.ScheduleTimeSlotField;
    /// <summary>
    /// Special field - schedule room slot
    /// </summary>
    public static bool IsRoomSlot(this ProjectField field) => field.FieldType == ProjectFieldType.ScheduleRoomField;
}
