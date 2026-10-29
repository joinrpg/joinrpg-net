namespace JoinRpg.XGameApi.Contract.Schedule;

/// <summary>
/// Schedule of project with modification time
/// </summary>
public class ScheduleInfoApi
{
    /// <summary>
    /// Time of last change of schedule as a whole (UTC). Changes when any program item is changed,
    /// added or removed, or when rooms or time slots are edited. Could also change without
    /// visible changes in schedule (for example, when other characters of project are edited).
    /// This is a hint for skipping unchanged schedule: change time is taken when edit starts,
    /// not when it is saved, so a long concurrent edit could be missed. Download full schedule
    /// periodically anyway.
    /// Does not track change of project time zone and change of author's display name in profile.
    /// </summary>
    public DateTimeOffset LastModifiedAt { get; set; }

    /// <summary>
    /// All scheduled program items
    /// </summary>
    public required IReadOnlyList<ProgramItemInfoApi> Items { get; set; }
}
