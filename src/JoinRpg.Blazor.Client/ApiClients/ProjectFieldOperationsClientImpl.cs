using JoinRpg.Web.ProjectMasterTools.Fields;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class ProjectFieldOperationsClientImpl(
    HttpClient httpClient,
    CsrfTokenProvider csrfTokenProvider,
    ILogger<ProjectFieldOperationsClientImpl> logger) : IProjectFieldOperationsClient
{
    public async Task Delete(ProjectFieldIdentification fieldId)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync(
                $"webapi/project-field-operations/delete?projectId={fieldId.ProjectId.Value}",
                fieldId);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during field delete");
            throw;
        }
    }
}
