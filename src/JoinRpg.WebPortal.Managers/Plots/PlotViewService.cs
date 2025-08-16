using JoinRpg.Data.Interfaces;
using JoinRpg.PrimitiveTypes.Plots;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Plots;

namespace JoinRpg.WebPortal.Managers.Plots;
internal class PlotViewService(IPlotRepository plotRepository, IPlotService plotService) : IPlotClient
{
    public Task DeleteElement(PlotElementIdentification elementId) => plotService.DeleteElement(elementId);

    public async Task<PlotFolderDto[]> GetPlotFoldersList(ProjectIdentification projectId)
    {
        var allFolders = await plotRepository.GetPlots(projectId);
        return [.. allFolders
            .Select(folder => new PlotFolderDto(new PlotFolderIdentification(projectId, folder.PlotFolderId), folder.MasterTitle))
            .OrderBy(folder => folder.Name)
            ];
    }

    public Task PublishVersion(PublishPlotElementViewModel model) => plotService.PublishElementVersion(model.PlotVersionId, model.SendNotification, model.CommentText);
    public Task UnDeleteElement(PlotElementIdentification elementId) => plotService.UnPublishElement(elementId);

    public async Task UnPublishVersion(PlotVersionIdentification version)
    {
        await plotService.UnPublishElement(version.PlotElementId);
    }
}
