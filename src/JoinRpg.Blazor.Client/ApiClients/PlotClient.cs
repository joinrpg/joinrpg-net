using JoinRpg.DomainTypes.Plots;
using JoinRpg.Web.Plots;
using JoinRpg.Web.Plots.Elements;
using JoinRpg.Web.Plots.Folders;

namespace JoinRpg.Blazor.Client.ApiClients;

public class PlotClient(HttpClient httpClient) : IPlotClient
{
    public async Task DeleteElement(PlotElementIdentification elementId)
        => await (await httpClient.PostAsync($"/webapi/plots/DeleteElement?projectId={elementId.ProjectId}&elementId={elementId}", new StringContent("")))
            .EnsureSuccessOrThrowProblem();

    public async Task UnDeleteElement(PlotElementIdentification elementId)
        => await (await httpClient.PostAsync($"/webapi/plots/UnDeleteElement?projectId={elementId.ProjectId}&elementId={elementId}", new StringContent("")))
            .EnsureSuccessOrThrowProblem();

    public async Task<PlotFolderDto[]> GetPlotFoldersList(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"/webapi/plots/GetPlotFoldersList?projectId={projectId.Value}"))
            .ReadFromJsonOrThrowProblem<PlotFolderDto[]>();

    public async Task PublishVersion(PublishPlotElementViewModel model)
        => await (await httpClient.PostAsJsonAsync($"/webapi/plots/PublishVersion?projectId={model.ProjectId}", model))
            .EnsureSuccessOrThrowProblem();

    public async Task UnPublishVersion(PlotVersionIdentification version)
        => await (await httpClient.PostAsync($"/webapi/plots/UnPublishVersion?projectId={version.ProjectId}&plotVersion={version}", new StringContent("")))
            .EnsureSuccessOrThrowProblem();
}
