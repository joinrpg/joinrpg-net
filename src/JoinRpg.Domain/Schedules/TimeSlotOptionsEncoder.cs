using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using Newtonsoft.Json;

namespace JoinRpg.Domain.Schedules;

/// <summary>
/// Extension methods for TimeSlot options extract/serializer
/// </summary>
public static class TimeSlotOptionsEncoder
{
    /// <summary>
    /// Extract Time Slot options from programmatic value
    /// </summary>
    public static TimeSlotOptions GetTimeSlotOptions(this ProjectFieldDropdownValue self)
    {
        if (!self.ProjectField.IsTimeSlot())
        {
            throw new Exception("That's not time slot'");
        }

        if (!(self.ProgrammaticValue is null))
        {
            return JsonConvert.DeserializeObject<TimeSlotOptions>(self.ProgrammaticValue) ?? GetDefaultTimeSlotOptions(self.ProjectField, self);
        }

        return GetDefaultTimeSlotOptions(self.ProjectField, self);
    }

    /// <summary>
    /// Get default time zone options (usable when you are creating field)
    /// </summary>
    public static TimeSlotOptions GetDefaultTimeSlotOptions(this ProjectField field)
        => GetDefaultTimeSlotOptions(field, variant: null);

    private static TimeSlotOptions GetDefaultTimeSlotOptions(ProjectField field,
        ProjectFieldDropdownValue? variant)
    {
        var prev = field.GetPreviousVariant(variant);

        if (prev?.ProgrammaticValue is null)
        {
            return TimeSlotOptions.CreateDefault();
        }

        var prevOptions = prev.GetTimeSlotOptions();
        return new TimeSlotOptions()
        {
            TimeSlotInMinutes = prevOptions.TimeSlotInMinutes,
            StartTime = prevOptions.StartTime.Add(prevOptions.TimeSlotLength)
                .AddMinutes(10),
        };
    }

    private static ProjectFieldDropdownValue? GetPreviousVariant([NotNull] this ProjectField field,
        ProjectFieldDropdownValue? variant)
    {
        if (variant is null)
        {
            return field.GetOrderedValues().LastOrDefault();
        }
        var variants = field.GetOrderedValues().ToList();
        var prevPosition = variants.IndexOf(variant) - 1;
        return prevPosition < 0 ? null : variants[prevPosition];
    }

    /// <summary>
    /// Save time slot options to variant
    /// </summary>
    public static void SetTimeSlotOptions(this ProjectFieldDropdownValue self, TimeSlotOptions? timeSlotOptions)
    {
        if (!self.ProjectField.IsTimeSlot())
        {
            throw new Exception("That's not time slot'");
        }

        self.ProgrammaticValue = JsonConvert.SerializeObject(timeSlotOptions);
    }
}
