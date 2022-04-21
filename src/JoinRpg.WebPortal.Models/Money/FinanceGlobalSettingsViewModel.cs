using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models;

public class FinanceGlobalSettingsViewModel
{
    public int ProjectId { get; set; }

    [Display(
        Name = "Предупреждать о переплате в заявках",
        Description =
            "Показывать предупреждение, если в заявке заплачено больше установленного взноса. Выключите, если вы собираете пожертвования и готовы принять любую сумму.")]
    public bool WarnOnOverPayment { get; set; }

    [Display(Name = "Включить льготный взнос",
        Description = "Включить возможность настройки пониженного взноса для некоторых категорий игроков. Мы рекомендуем предоставлять скидку студентам дневных отделений и школьникам.")]
    public bool PreferentialFeeEnabled { get; set; }

    [Display(Name = "Условия льготного взноса",
         Description = " Будет показываться игрокам, претендующим на льготный взнос."),
     UIHint("MarkdownString")]
    public string PreferentialFeeConditions { get; set; }
}
