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

    public async Task DeleteVariant(ProjectFieldVariantIdentification variantId)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync(
                $"webapi/project-field-operations/deletevariant?projectId={variantId.FieldId.ProjectId.Value}",
                variantId);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during field variant delete");
            throw;
        }
    }

    public async Task DeleteUnusedVariants(ProjectFieldIdentification fieldId)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync(
                $"webapi/project-field-operations/deleteunusedvariants?projectId={fieldId.ProjectId.Value}",
                fieldId);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during unused field variants delete");
            throw;
        }
    }

    public async Task CreateTimeSlots(TimeSlotMassAddRequest request)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync(
                $"webapi/project-field-operations/createtimeslots?projectId={request.FieldId.ProjectId.Value}",
                request);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during time slots mass creation");
            throw;
        }
    }

    public async Task SortTimeSlotsByStartTime(ProjectFieldIdentification fieldId)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync(
                $"webapi/project-field-operations/sorttimeslotsbystarttime?projectId={fieldId.ProjectId.Value}",
                fieldId);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during time slots sort");
            throw;
        }
    }
}
