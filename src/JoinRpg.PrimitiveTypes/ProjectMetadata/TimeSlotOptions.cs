namespace JoinRpg.PrimitiveTypes.ProjectMetadata;

/// <summary>
/// Additional time slot info for schedules
/// </summary>
public class TimeSlotOptions
{
    //Beware of field names - serialized to JSON

    /// <summary>
    /// Start of program item time (with TZ)
    /// </summary>
    public DateTimeOffset StartTime { get; set; }
    /// <summary>
    /// Length of time slot (for edit and serialization)
    /// </summary>
    public int TimeSlotInMinutes { get; set; }
    /// <summary>
    /// Length of time slot
    /// </summary>
    public TimeSpan TimeSlotLength => TimeSpan.FromMinutes(TimeSlotInMinutes);

    /// <summary>
    /// End of program item (with TZ)
    /// </summary>
    public DateTimeOffset EndTime => StartTime.Add(TimeSlotLength);
}
