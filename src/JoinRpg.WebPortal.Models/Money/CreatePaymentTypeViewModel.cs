using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Models;

public class CreatePaymentTypeViewModel : PaymentTypeViewModelBase
{

    [Display(Name = "Мастер", Description = "Укажите здесь мастера, которому принадлежит карточка, на которую будут переводить деньги")]
    public UserIdentification UserId { get; set; } = null!;
    [ReadOnly(true)]
    public IEnumerable<MasterViewModel> Masters { get; set; }
}
