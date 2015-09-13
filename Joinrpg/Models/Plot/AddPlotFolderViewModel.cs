using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models.Plot
{
  public class AddPlotFolderViewModel
  {
    [Required]
    public int ProjectId { get; set; }

    [ReadOnly(true)]
    public string ProjectName { get; set; }

    [Required, Display(Name="Название сюжета")]
    public string PlotFolderMasterTitle{ get; set; }

    [Display(Name = "TODO (что сделать по сюжету)"), DataType(DataType.MultilineText)]
    public string TodoField { get; set; }
  }
}
