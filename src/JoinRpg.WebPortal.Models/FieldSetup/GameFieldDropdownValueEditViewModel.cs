using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.Models.FieldSetup;

/// <summary>
/// View class for editing dropdown value
/// </summary>
public class GameFieldDropdownValueEditViewModel : GameFieldDropdownValueViewModelBase
{
    public bool IsActive { get; set; }

    public int ProjectFieldDropdownValueId { get; set; }

    public GameFieldDropdownValueEditViewModel(ProjectFieldInfo field, ProjectFieldVariant value) : base(field)
    {
        Label = value.Label;
        Description = value.Description.Contents;
        IsActive = value.IsActive;
        Price = value.Price;
        ProjectFieldDropdownValueId = value.Id.ProjectFieldVariantId;
        PlayerSelectable = value.IsPlayerSelectable;
        ProgrammaticValue = value.ProgrammaticValue;
        if (value is TimeSlotFieldVariant timeSlotFieldVariant)
        {
            TimeSlotInMinutes = timeSlotFieldVariant.TimeSlotOptions.TimeSlotInMinutes;
            TimeSlotStartTime = timeSlotFieldVariant.TimeSlotOptions.StartTime;
        }
    }

    public GameFieldDropdownValueEditViewModel() { }//For binding
}
