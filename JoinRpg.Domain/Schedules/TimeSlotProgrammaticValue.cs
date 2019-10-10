using System;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using Newtonsoft.Json;

namespace JoinRpg.Domain.Schedules
{
    /// <summary>
    /// Additional time slot info.
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
    }

    /// <summary>
    /// Extension methods for TimeSlot options extract/serializer
    /// </summary>
    public static class TimeSlotOptionsEncoder
    {
        public static TimeSlotOptions GetTimeSlotOptions(this ProjectFieldDropdownValue self)
        {
            if (!self.ProjectField.IsTimeSlot())
            {
                throw new Exception("That's not time slot'");
            }

            if (!(self.ProgrammaticValue is null))
            {
                return JsonConvert.DeserializeObject<TimeSlotOptions>(self.ProgrammaticValue);
            }

            return GetDefaultTimeSlotOptions(self.ProjectField, self);
        }

        public static TimeSlotOptions GetDefaultTimeSlotOptions(this ProjectField field)
            => GetDefaultTimeSlotOptions(field, variant: null);

        private static TimeSlotOptions GetDefaultTimeSlotOptions(ProjectField field,
            ProjectFieldDropdownValue variant)
        {
            var prev = field.GetPreviousVariant(variant);

            if (prev?.ProgrammaticValue is null)
            {
                return new TimeSlotOptions()
                {
                    StartTime = new DateTimeOffset(DateTime.Now, TimeSpan.FromHours(3)),
                    TimeSlotInMinutes = 50,
                };
            }

            var prevOptions = prev.GetTimeSlotOptions();
            return new TimeSlotOptions()
            {
                TimeSlotInMinutes = prevOptions.TimeSlotInMinutes,
                StartTime = prevOptions.StartTime.Add(prevOptions.TimeSlotLength)
                    .AddMinutes(10),
            };
        }

        [CanBeNull]
        private static ProjectFieldDropdownValue GetPreviousVariant([NotNull] this  ProjectField field,
            [CanBeNull] ProjectFieldDropdownValue variant)
        {
            if (variant is null)
            {
                return field.GetOrderedValues().LastOrDefault();
            }
            var variants = field.GetOrderedValues().ToList();
            var prevPosition = variants.IndexOf(variant) - 1;
            return prevPosition < 0 ? null : variants[prevPosition];
        }

        public static void SetTimeSlotOptions(this ProjectFieldDropdownValue self, TimeSlotOptions timeSlotOptions)
        {
            if (!self.ProjectField.IsTimeSlot())
            {
                throw new Exception("That's not time slot'");
            }

            self.ProgrammaticValue = JsonConvert.SerializeObject(timeSlotOptions);
        }
    }
}
