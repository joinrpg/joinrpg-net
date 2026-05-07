namespace JoinRpg.Web.Claims.UnifiedGrid;

public interface IUnifiedGridClient
{
    Task<IReadOnlyCollection<UgItemForCaptainViewModel>> GetForCaptain(ProjectIdentification projectId, UgStatusFilterView filter);
}
