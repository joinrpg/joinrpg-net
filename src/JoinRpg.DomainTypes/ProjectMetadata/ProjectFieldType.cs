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
    /// Новые члены дописываются только в конец: в БД лежит int, и <see cref="ProjectFieldType"/>
    /// кастуется напрямую из ProjectFieldViewType.
    /// </summary>
    UserLink,
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
