using System.ComponentModel.DataAnnotations;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.Finances;

namespace JoinRpg.Web.Finances.Expenses;

/// <summary>Строка сводки расходов по категории: суммы в разрезе состояний согласования.</summary>
public record ExpenseSummaryRowViewModel(
    string CategoryPath,
    decimal ApprovedPlannedCost,
    decimal ApprovedActualCost,
    decimal ProposedCost);

/// <summary>Мастер для выбора в форме расхода: идентификатор и отображаемое имя.</summary>
public record ExpenseMasterViewModel(
    UserIdentification MasterId,
    string Name);

/// <summary>
/// Расход для грида. CategoryPath — отображаемый путь категории через «\»
/// (например «Аренда базы\Жилые корпуса»), строится на стороне, отдающей данные.
/// </summary>
/// <param name="Description">Описание (markdown), сырой текст.</param>
/// <param name="MasterId">Мастер, понёсший расход; страница правит расход по id, а не по имени.</param>
public record ExpenseViewModel(
    ExpenseIdentification ExpenseId,
    string Name,
    string CategoryPath,
    int PlannedQuantity,
    decimal PlannedPrice,
    int? ActualQuantity,
    decimal? ActualPrice,
    DateOnly? SpentAt,
    ExpenseStateView State,
    string MasterName,
    string CreatedByName,
    string? Description = null,
    UserIdentification? MasterId = null)
{
    public decimal PlannedCost => ExpenseCost.Planned(PlannedQuantity, PlannedPrice);

    public decimal? ActualCost => ExpenseCost.Actual(ActualQuantity, ActualPrice);

    /// <summary>Ссылка на профиль мастера для колонки «Мастер» грида.</summary>
    public UserLinkViewModel MasterLink => new(MasterId, MasterName, ViewMode.Show);
}

/// <summary>
/// Модель формы создания/редактирования расхода. Description — сырой markdown-текст.
/// Границы цен — в <see cref="Validate"/>: RangeAttribute с decimal парсит границы
/// в текущей культуре и падает под не-инвариантной локалью.
/// </summary>
public class ExpenseEditViewModel : IValidatableObject
{
    /// <summary>Редактируемый расход; <c>null</c> — создание нового.</summary>
    public ExpenseIdentification? ExpenseId { get; set; }

    [Display(Name = "Название")]
    [Required(ErrorMessage = "Укажите название расхода")]
    public string Name { get; set; } = "";

    [Display(Name = "Категория",
        Description = "Путь через «\\»: например, Аренда базы\\Жилые корпуса")]
    [Required(ErrorMessage = "Укажите категорию расхода")]
    public string CategoryPath { get; set; } = "";

    [Display(Name = "Мастер", Description = "Мастер, который несёт расход")]
    [Required(ErrorMessage = "Выберите мастера")]
    public UserIdentification? MasterId { get; set; }

    [Display(Name = "Количество (план)")]
    [Range(1, int.MaxValue, ErrorMessage = "Плановое количество должно быть больше нуля")]
    public int PlannedQuantity { get; set; } = 1;

    [Display(Name = "Цена (план)")]
    public decimal PlannedPrice { get; set; }

    [Display(Name = "Количество (факт)")]
    [Range(1, int.MaxValue, ErrorMessage = "Фактическое количество должно быть больше нуля")]
    public int? ActualQuantity { get; set; }

    [Display(Name = "Цена (факт)")]
    public decimal? ActualPrice { get; set; }

    [Display(Name = "Дата")]
    public DateOnly? SpentAt { get; set; }

    [Display(Name = "Описание")]
    public string? Description { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PlannedPrice <= 0)
        {
            yield return new ValidationResult("Плановая цена должна быть больше нуля", [nameof(PlannedPrice)]);
        }
        if (!FitsMoneyColumn(PlannedPrice))
        {
            yield return new ValidationResult("Плановая цена — не больше двух знаков после запятой", [nameof(PlannedPrice)]);
        }
        if (ActualPrice is <= 0)
        {
            yield return new ValidationResult("Фактическая цена должна быть больше нуля", [nameof(ActualPrice)]);
        }
        if (ActualPrice is { } actualPrice && !FitsMoneyColumn(actualPrice))
        {
            yield return new ValidationResult("Фактическая цена — не больше двух знаков после запятой", [nameof(ActualPrice)]);
        }
    }

    /// <summary>Цена должна влезать в decimal(18,2) без округления: иначе SQL Server молча
    /// округлит лишние знаки или убьёт запрос arithmetic overflow.</summary>
    private static bool FitsMoneyColumn(decimal price)
        => price <= 9999999999999999.99m && price == decimal.Round(price, 2);
}
