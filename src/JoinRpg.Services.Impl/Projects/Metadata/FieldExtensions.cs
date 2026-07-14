using JoinRpg.DataModel;

namespace JoinRpg.Services.Impl.Projects.Metadata;

internal static class FieldExtensions
{
    public static bool HasValueList(this ProjectField field) => field.FieldType.HasValuesList();

    public static bool HasSpecialGroup(this ProjectField field) => field.HasValueList() && field.FieldBoundTo == FieldBoundTo.Character;

    public static string GetSpecialGroupName(this ProjectFieldDropdownValue fieldValue) => $"{fieldValue.Label}";

    public static string GetSpecialGroupName(this ProjectField field) => $"{field.FieldName}";

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
}
