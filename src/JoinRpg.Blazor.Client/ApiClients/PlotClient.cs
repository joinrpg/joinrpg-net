using JoinRpg.PrimitiveTypes.Plots;
using JoinRpg.Web.Plots;

namespace JoinRpg.Blazor.Client.ApiClients;

public class PlotClient(HttpClient httpClient) : IPlotClient
{
    public async Task DeleteElement(PlotElementIdentification elementId)
    {
        _ = (await httpClient.PostAsync($"/webapi/plots/DeleteElement?projectId={elementId.ProjectId}&elementId={elementId}", new StringContent(""))).EnsureSuccessStatusCode();
    }

    public async Task UnDeleteElement(PlotElementIdentification elementId)
    {
        _ = (await httpClient.PostAsync($"/webapi/plots/UnDeleteElement?projectId={elementId.ProjectId}&elementId={elementId}", new StringContent(""))).EnsureSuccessStatusCode();
    }

    public async Task<PlotFolderDto[]> GetPlotFoldersList(ProjectIdentification projectId)
    {
        return await httpClient.GetFromJsonAsync<PlotFolderDto[]>(
           $"/webapi/plots/GetPlotFoldersList?projectId={projectId.Value}")
           ?? throw new Exception("Couldn't get result from server");
    }

    public async Task PublishVersion(PublishPlotElementViewModel model)
    {
        (await httpClient.PostAsJsonAsync($"/webapi/plots/PublishVersion?projectId={model.ProjectId}", model)).EnsureSuccessStatusCode();
    }

    public async Task UnPublishVersion(PlotVersionIdentification version)
    {
        _ = (await httpClient.PostAsync($"/webapi/plots/UnPublishVersion?projectId={version.ProjectId}&plotVersion={version}", new StringContent(""))).EnsureSuccessStatusCode();
    }
}
