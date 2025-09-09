namespace JoinRpg.Web.Plots;

public record PlotFolderDto(PlotFolderIdentification PlotFolderId, string Name) : IPlotFolderLink;

public interface IPlotFolderLink
{
    PlotFolderIdentification PlotFolderId { get; }
    string Name { get; }
}
