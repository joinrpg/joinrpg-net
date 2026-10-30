using System.ComponentModel.DataAnnotations;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;

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

public class FinancePaymentTypesViewModel
{
    public required ProjectIdentification ProjectId { get; init; }
    public required ProjectLifecycleStatus ProjectStatus { get; init; }
    /// <summary>Есть право менять методы оплаты; без него панель только для просмотра.</summary>
    public required bool HasEditAccess { get; init; }
    /// <summary>В проекте настроен взнос — тогда без активного метода оплаты игроки не могут сдать деньги.</summary>
    public required bool HasFeeSettings { get; init; }
    /// <summary>Активные выше выключенных, основной первым, затем по виду и названию.</summary>
    public required IReadOnlyList<FinancePaymentTypeItemViewModel> PaymentTypes { get; init; }
}

/// <param name="PaymentTypeId"><c>null</c> — метода ещё нет в проекте (наличные мастера или онлайн-оплата): «Включить» создаёт его.</param>
/// <param name="Master">Ответственный мастер; у онлайн-оплаты — служебный пользователь платёжной системы.</param>
/// <param name="CanToggle">Текущий пользователь может включить или выключить метод: онлайн-оплату включает только админ сайта.</param>
public record FinancePaymentTypeItemViewModel(
    PaymentTypeIdentification? PaymentTypeId,
    PaymentTypeKind Kind,
    string Name,
    bool IsActive,
    bool IsDefault,
    UserLinkViewModel Master,
    bool CanToggle)
{
    public bool IsOnline => Kind is PaymentTypeKind.Online or PaymentTypeKind.OnlineSubscription;
}

public class AddPaymentTypeViewModel
{
    [Display(Name = "Название")]
    [Required(ErrorMessage = "Укажите название метода оплаты")]
    public string Name { get; set; } = "";

    [Display(Name = "Ответственный", Description = "Мастер, который принимает деньги этим методом")]
    [Required(ErrorMessage = "Выберите ответственного мастера")]
    public UserIdentification? MasterId { get; set; }
}

/// <summary>Включить метод, которого ещё нет в проекте: наличные мастера, онлайн-оплату или произвольный метод.</summary>
/// <param name="MasterId">Ответственный мастер; у онлайн-оплаты не нужен.</param>
/// <param name="Name">Название; нужно только произвольному методу.</param>
public record NewPaymentTypeViewModel(PaymentTypeKind Kind, UserIdentification? MasterId, string? Name);

/// <param name="ExportUri">Полная ссылка с личным токеном текущего мастера.</param>
public record FinanceGoogleDocsExportViewModel(Uri ExportUri);

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
