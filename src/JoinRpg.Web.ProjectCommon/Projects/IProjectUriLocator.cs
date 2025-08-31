namespace JoinRpg.Web.ProjectCommon.Projects;
public interface IProjectUriLocator
{
    Uri GetMyClaimUri(ProjectIdentification projectId);
    Uri GetAddClaimUri(ProjectIdentification projectId);
}
