using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models;

public abstract class ProjectFeeSettingViewModelBase
{
    [Display(Name = "Размер взноса")]
    public int Fee { get; set; }

    [Display(Name = "Размер льготного взноса")]
    public int? PreferentialFee { get; set; }

    [Display(Name = "Действует с")]
    public DateTime StartDate { get; set; }

    public int ProjectId { get; set; }
}
