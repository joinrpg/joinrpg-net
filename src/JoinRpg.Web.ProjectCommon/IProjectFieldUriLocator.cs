namespace JoinRpg.Web.ProjectCommon;

public interface IProjectFieldUriLocator
{
    Uri GetEditUri(ProjectFieldIdentification fieldId);
    Uri GetCreateVariantUri(ProjectFieldIdentification fieldId);
}
