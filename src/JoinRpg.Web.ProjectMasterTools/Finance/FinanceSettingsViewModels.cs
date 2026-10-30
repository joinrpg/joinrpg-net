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

public class FinanceFeeSettingsViewModel
{
    public required ProjectIdentification ProjectId { get; init; }
    public required ProjectLifecycleStatus ProjectStatus { get; init; }
    /// <summary>Есть право менять расписание взносов; без него панель только для просмотра.</summary>
    public required bool HasEditAccess { get; init; }
    public required bool PreferentialFeeEnabled { get; init; }
    /// <summary>Расписание взносов по возрастанию даты начала.</summary>
    public required IReadOnlyList<FinanceFeeSettingItemViewModel> Fees { get; init; }
}

/// <param name="CanDelete">Удалить можно только взнос, который ещё не вступил в силу.</param>
public record FinanceFeeSettingItemViewModel(
    int ProjectFeeSettingId,
    int Fee,
    int? PreferentialFee,
    DateOnly StartDate,
    bool CanDelete);

public class CreateFeeSettingViewModel
{
    [Display(Name = "Размер взноса")]
    [Range(0, int.MaxValue, ErrorMessage = "Размер взноса не может быть отрицательным")]
    public int Fee { get; set; }

    [Display(Name = "Размер льготного взноса")]
    [Range(0, int.MaxValue, ErrorMessage = "Размер льготного взноса не может быть отрицательным")]
    public int? PreferentialFee { get; set; }

    [Display(Name = "Действует с")]
    [Required(ErrorMessage = "Укажите, с какого числа действует взнос")]
    public DateOnly? StartDate { get; set; }
}
