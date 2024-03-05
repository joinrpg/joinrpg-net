namespace JoinRpg.PrimitiveTypes.ProjectMetadata;

public enum ProjectFieldType
{
    String,
    Text,
    Dropdown,
    Checkbox,
    MultiSelect,
    Header,
    Number,
    Login,
    ScheduleRoomField,
    ScheduleTimeSlotField,
    PinCode,
    Uri,
}

public enum FieldBoundTo
{
    Character,
    Claim,
}

public enum MandatoryStatus
{
    Optional,
    Recommended,
    Required,
}
