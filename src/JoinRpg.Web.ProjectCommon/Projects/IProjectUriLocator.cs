namespace JoinRpg.Web.ProjectCommon.Projects;
public interface IProjectUriLocator
{
    Uri GetMyClaimUri(ProjectIdentification projectId);
    Uri GetAddClaimUri(ProjectIdentification projectId);
    Uri GetCreatePlotUri(ProjectIdentification projectId);

    Uri GetRolesListUri(ProjectIdentification projectId);
    Uri GetCaptainCabinetUri(ProjectIdentification projectId);
}
