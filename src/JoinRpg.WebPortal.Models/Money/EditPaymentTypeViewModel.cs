using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models;

public class EditPaymentTypeViewModel : PaymentTypeViewModelBase
{
    [Display(Name = "Предлагать по умолчанию")]
    public bool IsDefault { get; set; }
    public int PaymentTypeId { get; set; }
}
