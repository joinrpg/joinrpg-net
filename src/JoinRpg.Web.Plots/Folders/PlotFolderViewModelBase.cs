using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Plots.Folders;

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
