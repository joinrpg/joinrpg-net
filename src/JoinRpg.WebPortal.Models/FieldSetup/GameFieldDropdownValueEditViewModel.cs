using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Schedules;

namespace JoinRpg.Web.Models.FieldSetup;

/// <summary>
/// View class for editing dropdown value
/// </summary>
public class GameFieldDropdownValueEditViewModel : GameFieldDropdownValueViewModelBase
{
    public bool IsActive { get; set; }

    public int ProjectFieldDropdownValueId { get; set; }

    public GameFieldDropdownValueEditViewModel(ProjectField field, ProjectFieldDropdownValue value) : base(field)
    {
        Label = value.Label;
        Description = value.Description.Contents;
        IsActive = value.IsActive;
        Price = value.Price;
        ProjectFieldDropdownValueId = value.ProjectFieldDropdownValueId;
        PlayerSelectable = value.PlayerSelectable;
        ProgrammaticValue = value.ProgrammaticValue;
        if (field.IsTimeSlot())
        {
            var options = value.GetTimeSlotOptions();
            TimeSlotInMinutes = options.TimeSlotInMinutes;
            TimeSlotStartTime = options.StartTime;
        }
    }

    public GameFieldDropdownValueEditViewModel() { }//For binding
}
