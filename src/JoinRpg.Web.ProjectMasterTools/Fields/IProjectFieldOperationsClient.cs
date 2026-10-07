namespace JoinRpg.Web.ProjectMasterTools.Fields;

public interface IProjectFieldOperationsClient
{
    Task Delete(ProjectFieldIdentification fieldId);
    Task DeleteVariant(ProjectFieldVariantIdentification variantId);
    Task DeleteUnusedVariants(ProjectFieldIdentification fieldId);
    Task CreateTimeSlots(TimeSlotMassAddRequest request);
    Task SortTimeSlotsByStartTime(ProjectFieldIdentification fieldId);
}
