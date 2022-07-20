using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl;

internal class CreateProjectService : ICreateProjectService
{
    private readonly ProjectService projectService;
    private readonly IFieldSetupService fieldSetupService;
    private readonly IAccommodationService accommodationService;
    private readonly ICharacterService characterService;

    public CreateProjectService(ProjectService projectService, IFieldSetupService fieldSetupService, IAccommodationService accommodationService, ICharacterService characterService)
    {
        this.projectService = projectService;
        this.fieldSetupService = fieldSetupService;
        this.accommodationService = accommodationService;
        this.characterService = characterService;
    }

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
                var name = await CreateField("Имя персонажа", ProjectFieldType.String, MandatoryStatus.Required);
                var description = await CreateField("Описание персонажа", ProjectFieldType.Text);
                await fieldSetupService.SetFieldSettingsAsync(new FieldSettingsRequest() { ProjectId = projectId, DescriptionField = description, NameField = name });
                await CreateTopLevelCharacterSlot(project, "Хочу на игру", name);
                break;
            case ProjectTypeDto.Convention:
                await SetupConventionParticipant(request, projectId);
                break;
            case ProjectTypeDto.ConventionProgram:
                await SetupConventionProgram(request, projectId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.ProjectType));
        }


        return projectId;

        async Task<ProjectFieldIdentification> CreateField(string fieldName,
            ProjectFieldType projectFieldType,
            MandatoryStatus mandatoryStatus = MandatoryStatus.Optional,
            string? fieldHint = null,
            bool canPlayerEdit = false)
        {
            return await fieldSetupService.AddField(
                                    new CreateFieldRequest(
                                        projectId,
                                        projectFieldType,
                                        fieldName,
                                        fieldHint: fieldHint ?? "",
                                        canPlayerEdit: canPlayerEdit,
                                        canPlayerView: true,
                                        isPublic: true,
                                        FieldBoundTo.Character,
                                        mandatoryStatus,
                                        Array.Empty<int>(),
                                        validForNpc: true,
                                        includeInPrint: true,
                                        showForUnapprovedClaims: canPlayerEdit,
                                        price: 0,
                                        masterFieldHint: "",
                                        programmaticValue: null)
                                    );
        }

        async Task SetupConventionProgram(CreateProjectRequest request, ProjectIdentification projectId)
        {
            await projectService.EditProject(
                new EditProjectRequest()
                {
                    AutoAcceptClaims = false,
                    ClaimApplyRules = "",
                    IsAcceptingClaims = false,
                    IsAccommodationEnabled = false,
                    MultipleCharacters = true,
                    ProjectAnnounce = "",
                    ProjectId = projectId,
                    ProjectName = request.ProjectName,
                    PublishPlot = false
                });
            var name = await CreateField("Название мероприятия", ProjectFieldType.String, MandatoryStatus.Required, canPlayerEdit: true);
            var description = await CreateField("Описание мероприятия", ProjectFieldType.Text, canPlayerEdit: true);
            await fieldSetupService.SetFieldSettingsAsync(new FieldSettingsRequest() { ProjectId = projectId, DescriptionField = description, NameField = name });
            _ = await CreateField("Время проведения мероприятия", ProjectFieldType.ScheduleTimeSlotField, fieldHint: "Здесь вы можете указать, когда проводится мероприятие. Настройте в свойствах поля возможное время проведения");
            _ = await CreateField("Место проведения мероприятия", ProjectFieldType.ScheduleRoomField, fieldHint: "Здесь вы можете указать, где проводится мероприятие. Настройте в свойствах поля конкретные помещения");
            await CreateTopLevelCharacterSlot(project, "Хочу заявить мероприятие", name);
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
            await projectService.EditProject(
                new EditProjectRequest()
                {
                    AutoAcceptClaims = true,
                    ClaimApplyRules = "",
                    IsAcceptingClaims = false,
                    IsAccommodationEnabled = true,
                    MultipleCharacters = false,
                    ProjectAnnounce = "",
                    ProjectId = projectId,
                    ProjectName = request.ProjectName,
                    PublishPlot = false
                });
            await accommodationService.SaveRoomTypeAsync(new ProjectAccommodationType()
            {
                Capacity = 1,
                Cost = 0,
                Description = new MarkdownString("Измените свойства поселения в настройках"),
                IsInfinite = true,
                IsPlayerSelectable = true,
                Name = "Вид поселения для примера",
                ProjectId = projectId,
                IsAutoFilledAccommodation = true,
            });

            await CreateTopLevelCharacterSlot(project, "Участник конвента", null);
        }

        async Task CreateTopLevelCharacterSlot(Project project, string slotName, ProjectFieldIdentification? name)
        {
            var fields = new Dictionary<int, string?>();
            if (name is not null)
            {
                fields.Add(name.ProjectFieldId, slotName);
            }
            await characterService.AddCharacter(new AddCharacterRequest(
                    project.ProjectId,
                    ParentCharacterGroupIds: new[] { project.RootGroup.CharacterGroupId },
                    CharacterTypeInfo:
                        new CharacterTypeInfo(
                            CharacterType.Slot,
                            IsHot: false,
                            SlotLimit: null,
                            SlotName: slotName,
                            CharacterVisibility.Public),
                    FieldValues: fields
                    ));
        }
    }


}
