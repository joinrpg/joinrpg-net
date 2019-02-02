using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.Plot
{
  public abstract class PlotFolderViewModelBase
  {
    [Required]
    public int ProjectId{ get; set; }

    [ReadOnly(true), Display(Name = "Название сюжета")]
    public string PlotFolderMasterTitle{ get; set; }

    [Display(Name = "TODO"), DataType(DataType.MultilineText), Description("Что сделать по сюжету")]
    public string TodoField
    { get; set; }

    [ReadOnly(true), Display(Name = "Статус")]
    public PlotStatus Status { get; set; }
  }

  public static class PlotStatusExts
  {
    public static PlotStatus GetStatus(this PlotFolder folder)
    {
      return folder.IsActive ? (folder.InWork ? PlotStatus.InWork : PlotStatus.Completed) : PlotStatus.Deleted;
    }

    public static PlotStatus GetStatus(this PlotElement e)
    {
      if (!e.IsActive) return PlotStatus.Deleted;
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
  }


  public enum PlotStatus
  {
    [Display(Name = "В работе")]
    InWork,
    [Display(Name = "Закончен")]
    Completed,
    [Display(Name = "Удален")]
    Deleted,
    [Display(Name="Есть новая версия")]
    HasNewVersion,
  }
}