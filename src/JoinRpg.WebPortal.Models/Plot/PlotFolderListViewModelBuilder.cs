using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Plots;

namespace JoinRpg.Web.Models.Plot;

public static class PlotFolderListViewModelBuilder
{
    public static PlotFolderListViewModel ToPlotFolderListViewModel(IEnumerable<PlotFolder> folders, ICurrentUserAccessor currentUser, ProjectInfo projectInfo, string title)
    {
        var plotAccessArguments = AccessArgumentsFactory.CreatePlot(projectInfo, currentUser);

        var items = folders.Select(f => CreateItem(f, plotAccessArguments)).ToList();

        return new PlotFolderListViewModel(projectInfo.ProjectId, items, plotAccessArguments, title);
    }

    private static PlotFolderListItemViewModel CreateItem(PlotFolder folder, PlotAccessArguments plotAccessArguments)
    {
        return new PlotFolderListItemViewModel(
            folder.GetId(),
            folder.MasterTitle,
            folder.Elements.Count(x => x.IsActive),
            folder.TodoField,
            [.. folder.PlotTags.Select(tag => tag.TagName).Order()],
            folder.GetStatus(),
            plotAccessArguments);
    }
}
