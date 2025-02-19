using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects;

internal partial class CreateProjectService
{

    private async Task SetupConventionProgram(CreateProjectRequest request, ProjectIdentification projectId, CharacterGroupIdentification rootCharacterGroupId)
    {
        var name = await CreateField(projectId, "Название мероприятия", ProjectFieldType.String, MandatoryStatus.Required, canPlayerEdit: true);
        var defaultChar = await CreateTopLevelCharacterSlot(projectId, rootCharacterGroupId, "Хочу заявить мероприятие", name);

        await SetProjectSettings(projectId, request.ProjectName, defaultChar, autoAcceptClaims: false, enableAccomodation: false);

        var description = await CreateField(projectId, "Описание мероприятия", ProjectFieldType.Text, canPlayerEdit: true);
        await fieldSetupService.SetFieldSettingsAsync(new FieldSettingsRequest() { ProjectId = projectId, DescriptionField = description, NameField = name });
        _ = await CreateField(projectId, "Время проведения мероприятия", ProjectFieldType.ScheduleTimeSlotField, fieldHint: "Здесь вы можете указать, когда проводится мероприятие. Настройте в свойствах поля возможное время проведения");
        _ = await CreateField(projectId, "Место проведения мероприятия", ProjectFieldType.ScheduleRoomField, fieldHint: "Здесь вы можете указать, где проводится мероприятие. Настройте в свойствах поля конкретные помещения");
    }

    private async Task SetupConventionParticipant(CreateProjectRequest request, ProjectIdentification projectId, CharacterGroupIdentification rootCharacterGroupId)
    {
        await fieldSetupService.SetFieldSettingsAsync(
            new FieldSettingsRequest()
            {
                ProjectId = projectId,
                DescriptionField = null,
                NameField = null
            });

        var defaultChar = await CreateTopLevelCharacterSlot(projectId, rootCharacterGroupId, "Участник конвента", null);
        await SetProjectSettings(projectId, request.ProjectName, defaultChar, autoAcceptClaims: true, enableAccomodation: true);

        await accommodationService.SaveRoomTypeAsync(new ProjectAccommodationType()
        {
            Capacity = 1,
            Cost = 0,
            Description = new MarkdownString("Измените свойства поселения в настройках"),
            IsInfinite = false,
            IsPlayerSelectable = true,
            Name = "Вид поселения для примера",
            ProjectId = projectId,
            IsAutoFilledAccommodation = false,
        });


    }

    private async Task SetupLarp(CreateProjectRequest request, ProjectIdentification projectId, CharacterGroupIdentification rootCharacterGroupId)
    {
        var name = await CreateField(projectId, "Имя персонажа", ProjectFieldType.String, MandatoryStatus.Required);
        var description = await CreateField(projectId, "Описание персонажа", ProjectFieldType.Text);
        await fieldSetupService.SetFieldSettingsAsync(new FieldSettingsRequest() { ProjectId = projectId, DescriptionField = description, NameField = name });
        var defaultChar = await CreateTopLevelCharacterSlot(projectId, rootCharacterGroupId, "Хочу на игру", name);

        await SetProjectSettings(projectId, request.ProjectName, defaultChar, autoAcceptClaims: false, enableAccomodation: false);
    }
}
