using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.Models.FieldSetup;

/// <summary>
/// View class for creating dropdown value
/// </summary>
public class GameFieldDropdownValueCreateViewModel : GameFieldDropdownValueViewModelBase
{
    public GameFieldDropdownValueCreateViewModel(ProjectFieldInfo field) : base(field)
    {
        Label = $"Вариант {field.Variants.Count + 1}";
        if (field.IsTimeSlot)
        {
            var options = GetDefaultTimeSlotOptions(field);
            TimeSlotInMinutes = options.TimeSlotInMinutes;
            TimeSlotStartTime = options.StartTime;
        }
    }

    private static TimeSlotOptions GetDefaultTimeSlotOptions(ProjectFieldInfo field)
    {
        var prev = field.LastVariant as TimeSlotFieldVariant;

        if (prev?.TimeSlotOptions is null)
        {
            return TimeSlotOptions.CreateDefault();
        }

        var prevOptions = prev.TimeSlotOptions;
        return new TimeSlotOptions()
        {
            TimeSlotInMinutes = prevOptions.TimeSlotInMinutes,
            StartTime = prevOptions.EndTime.AddMinutes(10),
        };
    }

    public GameFieldDropdownValueCreateViewModel() { }//For binding
}
