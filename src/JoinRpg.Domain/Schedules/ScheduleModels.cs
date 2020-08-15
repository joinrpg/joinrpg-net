using System;
using System.Collections.Generic;
using System.Linq;
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

    public class ProgramItemPlaced
    {
        public ProgramItemPlaced(ProgramItem programItem, List<ScheduleBuilder.ProgramItemSlot> slots)
        : this(programItem, slots.Select(x => x.Room).ToList(), slots.Select(x => x.TimeSlot).ToList())
        {
        }

        private ProgramItemPlaced(ProgramItem item,
            IReadOnlyCollection<ScheduleRoom> rooms,
            IReadOnlyCollection<TimeSlot> timeSlots)
        {
            ProgramItem = item;
            Rooms = rooms;
            StartTime = timeSlots.Min(x => x.Options.StartTime);
            EndTime = timeSlots.Max(x => x.Options.EndTime);
        }

        public DateTimeOffset EndTime { get; set; }

        public DateTimeOffset StartTime { get; set; }

        public IReadOnlyCollection<ScheduleRoom> Rooms { get; }
        public ProgramItem ProgramItem { get; set; } = null;
    }

    public class ScheduleItemAttribute
    {
        public MarkdownString Description { get; internal set; }
        public string Name { get; internal set; }
        public int Id { get; internal set; }
        public int SeqId { get; internal set; }
    }

    public class ScheduleRoom : ScheduleItemAttribute { }

    public class TimeSlot : ScheduleItemAttribute
    {
        public TimeSlotOptions Options { get; internal set; }
    }

    public class ScheduleResult
    {
        public IReadOnlyList<ProgramItem> NotScheduled { get; set; }
        public IReadOnlyList<ScheduleRoom> Rooms { get; set; }

        public IReadOnlyList<TimeSlot> TimeSlots { get; set; }

        public IReadOnlyList<ProgramItem> Conflicted { get; set; }

        public List<List<ProgramItem>> Slots { get; set; }

        public List<ProgramItemPlaced> AllItems { get; set; }
    }

}
