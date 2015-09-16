using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.Plot
{
  public abstract class PlotFolderViewModelBase
  {
    [Required]
    public int ProjectId{ get; set; }

    [Required, Display(Name="Название сюжета")]
    public string PlotFolderMasterTitle{ get; set; }

    [Display(Name = "TODO (что сделать по сюжету)"), DataType(DataType.MultilineText)]
    public string TodoField
    { get; set; }

    protected static PlotFolderStatus GetStatus(PlotFolder folder)
    {
      return folder.IsActive ? (folder.InWork ? PlotFolderStatus.InWork : PlotFolderStatus.Completed) : PlotFolderStatus.Deleted;
    }

    [ReadOnly(true), Display(Name = "Статус")]
    public PlotFolderStatus Status { get; set; }
  }


  public enum PlotFolderStatus
  {
    [Display(Name = "В работе")]
    InWork,
    [Display(Name = "Закончен")]
    Completed,
    [Display(Name = "Удален")]
    Deleted
  }
}