namespace JoinRpg.Web.Games.FieldSetup;

public interface IProjectFieldOperationsClient
{
    Task Delete(ProjectFieldIdentification fieldId);
}
