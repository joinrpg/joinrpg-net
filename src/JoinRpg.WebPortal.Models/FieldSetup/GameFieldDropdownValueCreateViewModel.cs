using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Schedules;

namespace JoinRpg.Web.Models.FieldSetup;

/// <summary>
/// View class for creating dropdown value
/// </summary>
public class GameFieldDropdownValueCreateViewModel : GameFieldDropdownValueViewModelBase
{
    public GameFieldDropdownValueCreateViewModel(ProjectField field) : base(field)
    {
        Label = $"Вариант {field.DropdownValues.Count + 1}";
        if (field.IsTimeSlot())
        {
            var options = field.GetDefaultTimeSlotOptions();
            TimeSlotInMinutes = options.TimeSlotInMinutes;
            TimeSlotStartTime = options.StartTime;
        }
    }

    public GameFieldDropdownValueCreateViewModel() { }//For binding
}
