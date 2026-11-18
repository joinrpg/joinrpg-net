using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Plots;
using JoinRpg.DataModel;
using JoinRpg.Web.Plots;

namespace JoinRpg.Web.Models.Plot;

public static class PlotStatusExts
{
    public static PlotStatus GetStatus(this PlotFolder folder) => folder.IsActive ? (folder.InWork ? PlotStatus.InWork : PlotStatus.Completed) : PlotStatus.Deleted;

    public static PlotStatus GetStatus(this PlotFolderDetailsDto folder) => folder.IsActive ? (folder.InWork ? PlotStatus.InWork : PlotStatus.Completed) : PlotStatus.Deleted;

    public static PlotStatus GetStatus(this PlotElementDetailsDto e)
    {
        if (!e.IsActive)
        {
            return PlotStatus.Deleted;
        }

        if (e.PublishedVersion is null)
        {
            return PlotStatus.InWork;
        }

        return e.IsLastVersionPublished ? PlotStatus.Completed : PlotStatus.HasNewVersion;
    }

    public static PlotStatus GetStatus(this PlotTextDto e)
    {
        if (!e.IsActive)
        {
            return PlotStatus.Deleted;
        }

        if (!e.HasPublished)
        {
            return PlotStatus.InWork;
        }
        if (e.Published && e.Latest)
        {
            return PlotStatus.Completed;
        }
        return PlotStatus.HasNewVersion;
    }
}
