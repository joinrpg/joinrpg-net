using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects;

internal partial class CreateProjectService
{
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
