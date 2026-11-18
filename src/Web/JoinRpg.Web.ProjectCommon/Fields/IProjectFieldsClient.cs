namespace JoinRpg.Web.ProjectCommon.Fields;

public interface IProjectFieldsClient
{
    Task<List<ProjectFieldDto>> GetProjectFields(ProjectIdentification projectId);
}
