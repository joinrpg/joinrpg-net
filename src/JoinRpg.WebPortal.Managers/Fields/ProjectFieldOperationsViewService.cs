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
}
