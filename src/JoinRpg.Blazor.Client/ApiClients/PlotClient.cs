using System.Net.Http.Json;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.Plots;

namespace JoinRpg.Blazor.Client.ApiClients;

public class PlotClient(HttpClient httpClient) : IPlotClient
{
    public async Task<PlotFolderDto[]> GetPlotFoldersList(ProjectIdentification projectId)
    {
        return await httpClient.GetFromJsonAsync<PlotFolderDto[]>(
           $"/webapi/plots/GetPlotFoldersList?projectId={projectId.Value}")
           ?? throw new Exception("Couldn't get result from server");
    }
}
