using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectMasterTools.Finance;

public class FinanceGeneralSettingsViewModel
{
    public required ProjectIdentification ProjectId { get; set; }
    public required ProjectLifecycleStatus ProjectStatus { get; init; }
    /// <summary>Есть право менять эти настройки; без него панель только для просмотра.</summary>
    public required bool HasEditAccess { get; init; }

    [Display(
        Name = "Предупреждать о переплате в заявках",
        Description =
            "Показывать предупреждение, если в заявке заплачено больше установленного взноса. Выключите, если вы собираете пожертвования и готовы принять любую сумму.")]
    public required bool WarnOnOverPayment { get; set; }

    [Display(Name = "Включить льготный взнос",
        Description = "Включить возможность настройки пониженного взноса для некоторых категорий игроков. Мы рекомендуем предоставлять скидку студентам дневных отделений и школьникам.")]
    public required bool PreferentialFeeEnabled { get; set; }

    [Display(Name = "Условия льготного взноса",
         Description = "Будет показываться игрокам, претендующим на льготный взнос.")]
    public required string PreferentialFeeConditions { get; set; }
}
