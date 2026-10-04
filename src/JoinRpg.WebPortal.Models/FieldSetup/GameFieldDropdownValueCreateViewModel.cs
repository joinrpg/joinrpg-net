namespace JoinRpg.WebPortal.Models.FieldSetup;

/// <summary>
/// View class for creating dropdown value
/// </summary>
public class GameFieldDropdownValueCreateViewModel : GameFieldDropdownValueViewModelBase
{
    public GameFieldDropdownValueCreateViewModel(ProjectFieldInfo field, TimeZoneInfo projectTimeZone) : base(field)
    {
        Label = $"Вариант {field.Variants.Count + 1}";
        if (field.IsTimeSlot)
        {
            var options = GetDefaultTimeSlotOptions(field, projectTimeZone);
            TimeSlotInMinutes = options.TimeSlotInMinutes;
            TimeSlotStartTime = options.LocalStartTime;
        }
    }

    private static TimeSlotOptions GetDefaultTimeSlotOptions(ProjectFieldInfo field, TimeZoneInfo projectTimeZone)
    {
        var prev = field.LastVariant as TimeSlotFieldVariant;

        if (prev?.TimeSlotOptions is null)
        {
            return TimeSlotOptions.CreateDefault(projectTimeZone);
        }

        var prevOptions = prev.TimeSlotOptions;
        return prevOptions with { LocalStartTime = prevOptions.LocalEndTime.AddMinutes(10) };
    }

    public GameFieldDropdownValueCreateViewModel() { }//For binding
}
