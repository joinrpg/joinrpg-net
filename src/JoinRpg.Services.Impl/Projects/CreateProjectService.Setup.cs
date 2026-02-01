using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects;

internal partial class CreateProjectService
{

    private async Task SetupConventionProgram(CreateProjectRequest request, ProjectIdentification projectId, CharacterGroupIdentification rootCharacterGroupId)
    {
        var name = await CreateField(projectId, "Название мероприятия", ProjectFieldType.String, MandatoryStatus.Required, canPlayerEdit: true);
        var defaultChar = await CreateTopLevelCharacterSlot(projectId, rootCharacterGroupId, "Хочу заявить мероприятие", name);

        await projectService.SetClaimSettings(projectId,
            new ProjectClaimSettings(defaultChar, StrictlyOneCharacter: false, AutoAcceptClaims: false, IsAcceptingClaims: false, IsPublicProject: true));

        var description = await CreateField(projectId, "Описание мероприятия", ProjectFieldType.Text, canPlayerEdit: true);
        await fieldSetupService.SetFieldSettingsAsync(new FieldSettingsRequest() { ProjectId = projectId, DescriptionField = description, NameField = name });
        _ = await CreateField(projectId, "Время проведения мероприятия", ProjectFieldType.ScheduleTimeSlotField, fieldHint: "Здесь вы можете указать, когда проводится мероприятие. Настройте в свойствах поля возможное время проведения");
        _ = await CreateField(projectId, "Место проведения мероприятия", ProjectFieldType.ScheduleRoomField, fieldHint: "Здесь вы можете указать, где проводится мероприятие. Настройте в свойствах поля конкретные помещения");

        await projectService.SetContactSettings(projectId, ProjectProfileRequirementSettings.AllNotRequired with { RequireTelegram = MandatoryStatus.Recommended });
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
        await projectService.SetAccommodationSettings(projectId, enableAccommodation: true);

        await projectService.SetClaimSettings(projectId,
            new ProjectClaimSettings(defaultChar, StrictlyOneCharacter: true, AutoAcceptClaims: true, IsAcceptingClaims: false, IsPublicProject: true));

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

        await projectService.SetContactSettings(projectId, ProjectProfileRequirementSettings.AllNotRequired with { RequireTelegram = MandatoryStatus.Recommended, RequireRealName = MandatoryStatus.Required });
    }

    private async Task SetupLarp(CreateProjectRequest request, ProjectIdentification projectId, CharacterGroupIdentification rootCharacterGroupId)
    {
        var name = await CreateField(projectId, "Имя персонажа", ProjectFieldType.String, MandatoryStatus.Required);
        var description = await CreateField(projectId, "Описание персонажа", ProjectFieldType.Text);
        await fieldSetupService.SetFieldSettingsAsync(new FieldSettingsRequest() { ProjectId = projectId, DescriptionField = description, NameField = name });
        var defaultChar = await CreateTopLevelCharacterSlot(projectId, rootCharacterGroupId, "Хочу на игру", name);

        await projectService.SetClaimSettings(projectId,
            new ProjectClaimSettings(defaultChar, StrictlyOneCharacter: true, AutoAcceptClaims: false, IsAcceptingClaims: false, IsPublicProject: true));

        await projectService.SetContactSettings(projectId, ProjectProfileRequirementSettings.AllNotRequired with { RequireTelegram = MandatoryStatus.Recommended });
    }
}
