using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Models;

public class CreatePaymentTypeViewModel : PaymentTypeViewModelBase
{

    [Display(Name = "Мастер", Description = "Укажите здесь мастера, которому принадлежит карточка, на которую будут переводить деньги")]
    public int UserId { get; set; }
    [ReadOnly(true)]
    public IEnumerable<MasterViewModel> Masters { get; set; }
}
