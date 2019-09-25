using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.Schedules
{
    public class ProgramItem
    {
        public int Id { get; internal set; }
        public string Name { get; internal set; }
        public MarkdownString Description { get; internal set; }
        public User[] Authors { get; internal set; }
        public int ProjectId { get; set; }
    }

    public class ScheduleItemAttribute
    {
        public MarkdownString Description { get; internal set; }
        public string Name { get; internal set; }
        public int Id { get; internal set; }
        public int SeqId { get; internal set; }
    }

    public class ScheduleRoom : ScheduleItemAttribute { }

    public class TimeSlot : ScheduleItemAttribute { }

    public class ScheduleResult
    {
        public IReadOnlyList<ProgramItem> NotScheduled { get; set; }
        public IReadOnlyList<ScheduleRoom> Rooms { get; set; }

        public IReadOnlyList<TimeSlot> TimeSlots { get; set; }

        public IReadOnlyList<ProgramItem> Conflicted { get; set; }

        public List<List<ProgramItem>> Slots { get; set; }
    }

}
