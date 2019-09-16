using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
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

        public IReadOnlyList<ProgramItem> Conflicted { get; set;  }

        public List<List<ProgramItem>> Slots { get; set; }
    }

    public class ScheduleBuilder
    {
        private ICollection<Character> characters;
        private ProjectField TimeSlotField;
        private ProjectField RoomField;

        public ScheduleBuilder(Project project, ICollection<Character> characters)
        {
            this.characters = characters;
            TimeSlotField = project.Details.ProjectScheduleSettings.TimeSlotField;
            RoomField = project.Details.ProjectScheduleSettings.RoomField;
        }

        private List<ProgramItem> NotScheduled { get; } = new List<ProgramItem>();

        private class ProgramItemSlot
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
        private List<ScheduleRoom> Rooms { get; set;  }

        private List<TimeSlot> TimeSlots { get; set; }

        private HashSet<ProgramItem> Conflicted { get; } = new HashSet<ProgramItem>();

        public ScheduleResult Build()
        {
            Rooms = InitializeList<ScheduleRoom>(RoomField.GetOrderedValues()).ToList();
            TimeSlots = InitializeList<TimeSlot>(TimeSlotField.GetOrderedValues()).ToList();
            Slots = InitializeSlots(TimeSlots, Rooms);

            foreach (var character in characters)
            {
                var programItem = ConvertToProgramItem(character);
                var slots = SelectSlots(character);
                PutItem(programItem, slots);
            }
            return new ScheduleResult()
            {
                Conflicted = Conflicted.ToList(),
                NotScheduled = NotScheduled.ToList(),
                Rooms = Rooms,
                TimeSlots = TimeSlots,
                Slots = Slots.Select(row => row.Select(r => r.ProgramItem).ToList()).ToList(),
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

        private List<ProgramItemSlot> SelectSlots(Character character)
        {
            var fields = character.GetFields();

            IEnumerable<int> GetSlotIndexes(ProjectField field, IEnumerable<ScheduleItemAttribute> Items)
            {
                var variantIds = fields
                    .Single(f => f.Field.ProjectFieldId == field.ProjectFieldId)
                    .GetDropdownValues()
                    .Select(variant => variant.ProjectFieldDropdownValueId)
                    .ToList();
                return from item in Items where variantIds.Contains(item.Id) select item.SeqId;
            }

            var slots = from timeSeqId in GetSlotIndexes(TimeSlotField, TimeSlots)
                        from roomSeqId in GetSlotIndexes(RoomField, Rooms)
                        select Slots[timeSeqId][roomSeqId];

            return slots.ToList();
        }

        private List<List<ProgramItemSlot>> InitializeSlots(List<TimeSlot> timeSlots, List<ScheduleRoom> rooms)
        {
            var x = new List<List<ProgramItemSlot>>();
            foreach (var time in timeSlots)
            {
                x.Add(rooms.Select(room => new ProgramItemSlot(time, room)).ToList());
            }
            return x;
        }

        private IEnumerable<T> InitializeList<T>(IReadOnlyList<ProjectFieldDropdownValue> readOnlyList)
            where T: ScheduleItemAttribute, new ()
        {
            var seqId = 0;
            foreach (var variant in readOnlyList)
            {
                yield return new T()
                {
                    Id = variant.ProjectFieldDropdownValueId,
                    Name = variant.Label,
                    Description = variant.Description,
                    SeqId = seqId,
                };
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
                Authors = new[] { character.ApprovedClaim.Player },
                ProjectId = character.ProjectId,
            };
        }
    }
}
