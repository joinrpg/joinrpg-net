using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models;

public abstract class PaymentTypeViewModelBase
{
    public int ProjectId { get; set; }
    [Display(Name = "Название метода оплаты"), Required]
    public string Name { get; set; }
}
