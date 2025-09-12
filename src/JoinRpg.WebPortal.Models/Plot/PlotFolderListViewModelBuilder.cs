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
        int elementsCount;
        if (plotAccessArguments.HasMasterAccess)
        {
            elementsCount = folder.Elements.Count(x => x.IsActive);
        }
        else
        {
            // Если нет мастерского доступа, показываем только опубликованные вводные
            elementsCount = folder.Elements.Count(x => x.Published != null && x.IsActive);
        }

        return new PlotFolderListItemViewModel(
            folder.GetId(),
            folder.MasterTitle,
            elementsCount,
            folder.TodoField,
            [.. folder.PlotTags.Select(tag => tag.TagName).Order()],
            folder.GetStatus(),
            plotAccessArguments);
    }
}
