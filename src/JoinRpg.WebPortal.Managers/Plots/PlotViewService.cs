using JoinRpg.Data.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.Plots;

namespace JoinRpg.WebPortal.Managers.Plots;
internal class PlotViewService(IPlotRepository plotRepository) : IPlotClient
{
    public async Task<PlotFolderDto[]> GetPlotFoldersList(ProjectIdentification projectId)
    {
        var allFolders = await plotRepository.GetPlots(projectId);
        return [.. allFolders
            .Select(folder => new PlotFolderDto(new PrimitiveTypes.Plots.PlotFolderIdentification(projectId, folder.PlotFolderId), folder.MasterTitle))
            .OrderBy(folder => folder.Name)
            ];
    }
}
