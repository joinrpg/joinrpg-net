using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Plots;

namespace JoinRpg.Web.Models.Plot;

public abstract class PlotFolderViewModelBase
{
    [Required]
    public int ProjectId { get; set; }

    [ReadOnly(true), Display(Name = "Название сюжета")]
    public string PlotFolderMasterTitle { get; set; }

    [Display(Name = "TODO"), DataType(DataType.MultilineText), Description("Что сделать по сюжету")]
    public string TodoField
    { get; set; }

    [ReadOnly(true), Display(Name = "Статус")]
    public PlotStatus Status { get; set; }
}

public static class PlotStatusExts
{
    public static PlotStatus GetStatus(this PlotFolder folder) => folder.IsActive ? (folder.InWork ? PlotStatus.InWork : PlotStatus.Completed) : PlotStatus.Deleted;

    public static PlotStatus GetStatus(this PlotElement e)
    {
        if (!e.IsActive)
        {
            return PlotStatus.Deleted;
        }

        if (e.Published == null)
        {
            return PlotStatus.InWork;
        }
        if (e.LastVersion().Version == e.Published)
        {
            return PlotStatus.Completed;
        }
        return PlotStatus.HasNewVersion;
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
