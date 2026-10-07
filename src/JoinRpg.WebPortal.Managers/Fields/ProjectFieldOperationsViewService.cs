using JoinRpg.Services.Interfaces;
using JoinRpg.Web.ProjectMasterTools.Fields;

namespace JoinRpg.WebPortal.Managers.Fields;

internal class ProjectFieldOperationsViewService(IFieldSetupService fieldSetupService) : IProjectFieldOperationsClient
{
    public async Task Delete(ProjectFieldIdentification fieldId)
        => await fieldSetupService.DeleteField(fieldId.ProjectId.Value, fieldId.ProjectFieldId);

    public async Task DeleteVariant(ProjectFieldVariantIdentification variantId)
        => _ = await fieldSetupService.DeleteFieldValueVariant(
            variantId.FieldId.ProjectId.Value,
            variantId.FieldId.ProjectFieldId,
            variantId.ProjectFieldVariantId);

    public async Task DeleteUnusedVariants(ProjectFieldIdentification fieldId)
        => _ = await fieldSetupService.DeleteUnusedFieldValueVariants(fieldId);

    public async Task CreateTimeSlots(TimeSlotMassAddRequest request)
        => await fieldSetupService.CreateTimeSlotVariants(new CreateTimeSlotVariantsRequest(
            request.FieldId,
            request.Prefix,
            request.Date,
            request.StartTime,
            request.EndTime,
            request.TimeSlotInMinutes,
            request.BreakInMinutes));

    public async Task SortTimeSlotsByStartTime(ProjectFieldIdentification fieldId)
        => await fieldSetupService.SortTimeSlotVariantsByStartTime(fieldId);

    public async Task SortVariantsByLabel(ProjectFieldIdentification fieldId)
        => await fieldSetupService.SortFieldVariants(fieldId.ProjectId.Value, fieldId.ProjectFieldId);

    public async Task CreateVariants(FieldValuesMassAddRequest request)
        => await fieldSetupService.CreateFieldValueVariants(request.FieldId, request.ValuesToAdd);
}
