using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;

namespace JoinRpg.Services.Impl.Projects;
internal partial class CreateProjectService
{
    private async Task<ProjectFieldIdentification> CreateField(
        ProjectIdentification projectId,
        string fieldName,
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
                                    [],
                                    validForNpc: true,
                                    includeInPrint: true,
                                    showForUnapprovedClaims: canPlayerEdit,
                                    price: 0,
                                    masterFieldHint: "",
                                    programmaticValue: null)
                                );
    }

    private async Task<CharacterIdentification> CreateTopLevelCharacterSlot(
        ProjectIdentification projectId,
        CharacterGroupIdentification rootCharacterGroupId,
        string slotName,
        ProjectFieldIdentification? name)
    {
        var fields = new Dictionary<int, string?>();
        if (name is not null)
        {
            fields.Add(name.ProjectFieldId, slotName);
        }
        return await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [rootCharacterGroupId],
                CharacterTypeInfo: CharacterTypeInfo.DefaultSlot(slotName),
                FieldValues: fields
                ));
    }

    private async Task SetProjectSettings(ProjectIdentification projectId, ProjectName projectName, CharacterIdentification? defaultChar,
        bool autoAcceptClaims, bool enableAccomodation)
    {
        await projectService.EditProject(
            new EditProjectRequest()
            {
                AutoAcceptClaims = autoAcceptClaims,
                ClaimApplyRules = "",
                IsAcceptingClaims = false,
                IsAccommodationEnabled = enableAccomodation,
                MultipleCharacters = true,
                ProjectAnnounce = "",
                ProjectId = projectId,
                ProjectName = projectName,
                DefaultTemplateCharacterId = defaultChar,
            });
    }
}
