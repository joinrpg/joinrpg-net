namespace JoinRpg.Web.ProjectMasterTools.Fields;

public interface IProjectFieldOperationsClient
{
    Task Delete(ProjectFieldIdentification fieldId);
}
