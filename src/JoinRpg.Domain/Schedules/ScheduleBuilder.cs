using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.Schedules
{
    /// <summary>
    /// Builds schedule from program item data
    /// </summary>
    public class ScheduleBuilder
    {
        private readonly ICollection<Character> characters;
        private ProjectField TimeSlotField { get; }

        private ProjectField RoomField { get; }

        public ScheduleBuilder(Project project, ICollection<Character> characters)
        {
            this.characters = characters.Where(ch => ch.IsActive).ToList();
            var scheduleSettings = project.Details.ScheduleSettings;
            if (scheduleSettings is null)
            {
                throw new Exception("Schedule not enabled");
            }
            TimeSlotField = scheduleSettings.TimeSlotField;
            RoomField = scheduleSettings.RoomField;
        }

        private HashSet<ProgramItem> NotScheduled { get; } = new HashSet<ProgramItem>();

        public class ProgramItemSlot
        {
            public ProgramItemSlot(TimeSlot slot, ScheduleRoom room)
            {
                Room = room;
                TimeSlot = slot;
            }
            public ScheduleRoom Room { get; }
            public TimeSlot TimeSlot { get; }
            public ProgramItem ProgramItem { get; set; } = null;
        }

        private List<List<ProgramItemSlot>> Slots { get; set; }
        private List<ScheduleRoom> Rooms { get; set; }

        private List<TimeSlot> TimeSlots { get; set; }

        private HashSet<ProgramItem> Conflicted { get; } = new HashSet<ProgramItem>();

        public ScheduleResult Build()
        {
            Rooms = InitializeList<ScheduleRoom>(RoomField.GetOrderedValues()).ToList();
            TimeSlots = InitializeList<TimeSlot>(TimeSlotField.GetOrderedValues()).ToList();
            Slots = InitializeSlots(TimeSlots, Rooms);

            var allItems = new List<ProgramItemPlaced>();

            foreach (var character in characters)
            {
                var programItem = ConvertToProgramItem(character);
                var slots = SelectSlots(programItem, character);
                PutItem(programItem, slots);
                if (slots.Any())
                {
                    allItems.Add(new ProgramItemPlaced(programItem, slots));
                }
            }
            return new ScheduleResult()
            {
                Conflicted = Conflicted.ToList(),
                NotScheduled = NotScheduled.ToList(),
                Rooms = Rooms,
                TimeSlots = TimeSlots,
                Slots = Slots.Select(row => row.Select(r => r.ProgramItem).ToList()).ToList(),
                AllItems = allItems,
            };
        }

        private void PutItem(ProgramItem programItem, List<ProgramItemSlot> slots)
        {
            if (!slots.Any())
            {
                NotScheduled.Add(programItem);
                return;
            }

            var conflicts = (from slot in slots
                             where slot.ProgramItem != null
                             select slot.ProgramItem).Distinct().ToList();

            if (conflicts.Any())
            {
                Conflicted.Add(programItem);
                foreach (var conflict in conflicts)
                {
                    Conflicted.Add(conflict);
                }
            }
            else
            {
                foreach (var slot in slots)
                {
                    slot.ProgramItem = programItem;
                }
            }
        }

        private List<ProgramItemSlot> SelectSlots(ProgramItem programItem, Character character)
        {
            var fields = character.GetFields();

            List<int> GetSlotIndexes(ProjectField field, IEnumerable<ScheduleItemAttribute> items)
            {
                var variantIds = fields
                    .Single(f => f.Field.ProjectFieldId == field.ProjectFieldId)
                    .GetDropdownValues()
                    .Select(variant => variant.ProjectFieldDropdownValueId)
                    .ToList();
                var indexes = (from item in items where variantIds.Contains(item.Id) select item.SeqId).ToList();
                if (indexes.Count < variantIds.Count) // Some variants not found, probably deleted
                {
                    NotScheduled.Add(programItem);
                }

                return indexes;
            }

            var slots = from timeSeqId in GetSlotIndexes(TimeSlotField, TimeSlots)
                        from roomSeqId in GetSlotIndexes(RoomField, Rooms)
                        select Slots[timeSeqId][roomSeqId];

            return slots.ToList();
        }

        private List<List<ProgramItemSlot>> InitializeSlots(List<TimeSlot> timeSlots, List<ScheduleRoom> rooms)
            => timeSlots.Select(time => rooms.Select(room => new ProgramItemSlot(time, room)).ToList()).ToList();

        private IEnumerable<T> InitializeList<T>(IReadOnlyList<ProjectFieldDropdownValue> readOnlyList)
            where T : ScheduleItemAttribute, new()
        {
            var seqId = 0;
            foreach (var variant in readOnlyList.Where(x => x.IsActive))
            {
                var item = new T()
                {
                    Id = variant.ProjectFieldDropdownValueId,
                    Name = variant.Label,
                    Description = variant.Description,
                    SeqId = seqId,
                };
                if (item is TimeSlot timeSlot)
                {
                    timeSlot.Options = variant.GetTimeSlotOptions();
                }
                yield return item;
                seqId++;
            }
        }

        private ProgramItem ConvertToProgramItem(Character character)
        {
            return new ProgramItem()
            {
                Id = character.CharacterId,
                Name = character.CharacterName,
                Description = character.Description,
                Authors = new[] { character.ApprovedClaim?.Player }.Where(x => !(x is null)).ToArray(),
                ProjectId = character.ProjectId,
            };
        }
    }
}
