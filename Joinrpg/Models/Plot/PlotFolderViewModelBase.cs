using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models.Plot
{
  public class PlotFolderViewModelBase
  {
    [Required]
    public int ProjectId{ get; set; }

    [Required, Display(Name="Название сюжета")]
    public string PlotFolderMasterTitle{ get; set; }

    [Display(Name = "TODO (что сделать по сюжету)"), DataType(DataType.MultilineText)]
    public string TodoField { get; set; }
  }
}