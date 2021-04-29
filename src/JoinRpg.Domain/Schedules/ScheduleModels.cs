using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace JoinRpg.Domain.Schedules
{
    public class ProgramItem
    {
        public int Id { get; internal set; }
        public string Name { get; internal set; }
        public MarkdownString Description { get; internal set; }
        public User[] Authors { get; internal set; }
        public int ProjectId { get; set; }

        public ProgramItem(Character character)
        {
            Id = character.CharacterId;
            Name = character.CharacterName;
            Description = character.Description;
            Authors = new[] { character.ApprovedClaim?.Player }.WhereNotNull().ToArray();
            ProjectId = character.ProjectId;
        }
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
        public ProgramItem ProgramItem { get; set; }
    }

    // TODO: Invent way to fix it
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public record ScheduleResult(
        IReadOnlyList<ProgramItem> NotScheduled,
        IReadOnlyList<ScheduleRoom> Rooms,

        IReadOnlyList<TimeSlot> TimeSlots,

        IReadOnlyList<ProgramItem> Conflicted,

        List<List<ProgramItem?>> Slots,

        List<ProgramItemPlaced> AllItems)
    { }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
