
using JoinRpg.Web.Plots.Elements;
using JoinRpg.Web.Plots.Folders;

namespace JoinRpg.Web.Plots;

public interface IPlotClient
{
    Task<PlotFolderDto[]> GetPlotFoldersList(ProjectIdentification projectId);
    Task UnPublishVersion(PlotVersionIdentification version);

    Task PublishVersion(PublishPlotElementViewModel model);

    Task DeleteElement(PlotElementIdentification elementId);
    Task UnDeleteElement(PlotElementIdentification elementId);
}
