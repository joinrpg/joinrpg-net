using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects;

internal partial class CreateProjectService
    (ProjectService projectService,
    IFieldSetupService fieldSetupService,
    IAccommodationService accommodationService,
    ICharacterService characterService,
    IProjectMetadataRepository projectMetadataRepository,
    IProjectRepository projectRepository) : ICreateProjectService
{

    //TODO[Localize]
    async Task<ProjectIdentification> ICreateProjectService.CreateProject(CreateProjectRequest request)
    {
        var project = await projectService.AddProject(
            request.ProjectName,
            rootCharacterGroupName: "Все роли");

        var projectId = new ProjectIdentification(project.ProjectId);

        switch (request.ProjectType)
        {
            case ProjectTypeDto.Larp:
                await SetupLarp(request, project, projectId);
                break;
            case ProjectTypeDto.Convention:
                await SetupConventionParticipant(request, projectId);
                break;
            case ProjectTypeDto.ConventionProgram:
                await SetupConventionProgram(request, projectId);
                break;
            case ProjectTypeDto.CopyFromAnother:
                await CopyFromAnother(request.ProjectName, project, projectId, request.CopyFromId!);
                break;
            case ProjectTypeDto.EmptyProject:
                // Ничего не делаем
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.ProjectType));
        }


        return projectId;



        async Task SetupConventionProgram(CreateProjectRequest request, ProjectIdentification projectId)
        {
            var name = await CreateField(projectId, "Название мероприятия", ProjectFieldType.String, MandatoryStatus.Required, canPlayerEdit: true);
            var defaultChar = await CreateTopLevelCharacterSlot(project, "Хочу заявить мероприятие", name);

            await SetProjectSettings(projectId, request.ProjectName, defaultChar, autoAcceptClaims: false, enableAccomodation: false);

            var description = await CreateField(projectId, "Описание мероприятия", ProjectFieldType.Text, canPlayerEdit: true);
            await fieldSetupService.SetFieldSettingsAsync(new FieldSettingsRequest() { ProjectId = projectId, DescriptionField = description, NameField = name });
            _ = await CreateField(projectId, "Время проведения мероприятия", ProjectFieldType.ScheduleTimeSlotField, fieldHint: "Здесь вы можете указать, когда проводится мероприятие. Настройте в свойствах поля возможное время проведения");
            _ = await CreateField(projectId, "Место проведения мероприятия", ProjectFieldType.ScheduleRoomField, fieldHint: "Здесь вы можете указать, где проводится мероприятие. Настройте в свойствах поля конкретные помещения");
        }

        async Task SetupConventionParticipant(CreateProjectRequest request, ProjectIdentification projectId)
        {
            await fieldSetupService.SetFieldSettingsAsync(
                new FieldSettingsRequest()
                {
                    ProjectId = projectId,
                    DescriptionField = null,
                    NameField = null
                });

            var defaultChar = await CreateTopLevelCharacterSlot(project, "Участник конвента", null);
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

        async Task SetupLarp(CreateProjectRequest request, Project project, ProjectIdentification projectId)
        {
            var name = await CreateField(projectId, "Имя персонажа", ProjectFieldType.String, MandatoryStatus.Required);
            var description = await CreateField(projectId, "Описание персонажа", ProjectFieldType.Text);
            await fieldSetupService.SetFieldSettingsAsync(new FieldSettingsRequest() { ProjectId = projectId, DescriptionField = description, NameField = name });
            var defaultChar = await CreateTopLevelCharacterSlot(project, "Хочу на игру", name);

            await SetProjectSettings(projectId, request.ProjectName, defaultChar, autoAcceptClaims: false, enableAccomodation: false);
        }
    }

    private async Task CopyFromAnother(ProjectName projectName, Project project, ProjectIdentification projectId, ProjectIdentification copyFromId)
    {
        var original = await projectMetadataRepository.GetProjectMetadata(copyFromId);
        var originalEntity = await projectRepository.GetProjectAsync(copyFromId);
        await CopyFields(projectId, original);

        await projectService.EditProject(
            new EditProjectRequest()
            {
                AutoAcceptClaims = originalEntity.Details.AutoAcceptClaims,
                ClaimApplyRules = originalEntity.Details.ClaimApplyRules?.Contents ?? "",
                IsAcceptingClaims = false,
                IsAccommodationEnabled = original.AccomodationEnabled,
                MultipleCharacters = originalEntity.Details.EnableManyCharacters,
                ProjectAnnounce = originalEntity.Details.ProjectAnnounce?.Contents ?? "",
                ProjectId = projectId,
                ProjectName = projectName,
                PublishPlot = false,
                DefaultTemplateCharacterId = null, // TODO copy slots
            });
    }

    private async Task CopyFields(ProjectIdentification projectId, ProjectInfo original)
    {
        ProjectFieldIdentification? description = null;
        ProjectFieldIdentification? name = null;
        foreach (var field in original.UnsortedFields.Where(f => f.IsActive))
        {
            var newId = await fieldSetupService.AddField(
                        new CreateFieldRequest(
                            projectId,
                            field.Type,
                            field.Name,
                            field.Description?.Contents ?? "",
                            field.CanPlayerEdit,
                            field.CanPlayerView,
                            field.IsPublic,
                            field.BoundTo,
                            field.MandatoryStatus,
                            [], //field.GroupsAvailableForIds, TODO научиться копировать группы
                            field.ValidForNpc,
                            field.IncludeInPrint,
                            field.ShowOnUnApprovedClaims,
                            field.Price,
                            field.MasterDescription?.Contents ?? "",
                            field.ProgrammaticValue)
                        );
            if (field.HasValueList)
            {
                foreach (var variant in field.Variants.Where(v => v.IsActive))
                {
                    await fieldSetupService.CreateFieldValueVariant(new CreateFieldValueVariantRequest(newId, variant.Label, variant.Description?.Contents, variant.MasterDescription?.Contents, variant.ProgrammaticValue, variant.Price, variant.IsPlayerSelectable, (variant as TimeSlotFieldVariant)?.TimeSlotOptions));
                }
            }

            if (field.IsDescription)
            {
                description = newId;
            }

            if (field.IsName)
            {
                name = newId;
            }
        }

        await fieldSetupService.SetFieldSettingsAsync(new FieldSettingsRequest() { ProjectId = projectId, DescriptionField = description, NameField = name });
    }
}
