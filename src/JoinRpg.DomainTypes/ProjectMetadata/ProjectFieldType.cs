namespace JoinRpg.DomainTypes.ProjectMetadata;

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
    /// <summary>
    /// Ссылка на пользователя сайта. Значение — id пользователя.
    /// </summary>
    UserLink,
    /// <summary>
    /// Ссылки на нескольких пользователей сайта. Значение — id через запятую.
    /// </summary>
    MultiUserLink,
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
