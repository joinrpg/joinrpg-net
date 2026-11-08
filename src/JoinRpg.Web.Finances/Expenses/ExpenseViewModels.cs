using System.ComponentModel.DataAnnotations;
using JoinRpg.DomainTypes.Finances;

namespace JoinRpg.Web.Finances.Expenses;

/// <summary>Строка сводки расходов по категории: суммы в разрезе состояний согласования.</summary>
public record ExpenseSummaryRowViewModel(
    string CategoryPath,
    int ApprovedPlannedCost,
    int ApprovedActualCost,
    int ProposedCost);

/// <summary>Мастер для выбора в форме расхода: идентификатор и отображаемое имя.</summary>
public record ExpenseMasterViewModel(
    UserIdentification MasterId,
    string Name);

/// <summary>
/// Расход для грида. CategoryPath — отображаемый путь категории через «\»
/// (например «Аренда базы\Жилые корпуса»), строится на стороне, отдающей данные.
/// </summary>
/// <param name="Description">Описание (markdown), сырой текст.</param>
public record ExpenseViewModel(
    ExpenseIdentification ExpenseId,
    string Name,
    string CategoryPath,
    int PlannedQuantity,
    int PlannedPrice,
    int? ActualQuantity,
    int? ActualPrice,
    DateOnly? SpentAt,
    ExpenseStateView State,
    string MasterName,
    string CreatedByName,
    string? Description = null)
{
    public int PlannedCost => PlannedQuantity * PlannedPrice;

    public int? ActualCost => ActualQuantity is { } quantity && ActualPrice is { } price ? quantity * price : null;
}

/// <summary>
/// Модель формы создания/редактирования расхода. Description — сырой markdown-текст.
/// </summary>
public class ExpenseEditViewModel
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
    [Range(1, int.MaxValue, ErrorMessage = "Плановая цена должна быть больше нуля")]
    public int PlannedPrice { get; set; }

    [Display(Name = "Количество (факт)")]
    [Range(1, int.MaxValue, ErrorMessage = "Фактическое количество должно быть больше нуля")]
    public int? ActualQuantity { get; set; }

    [Display(Name = "Цена (факт)")]
    [Range(1, int.MaxValue, ErrorMessage = "Фактическая цена должна быть больше нуля")]
    public int? ActualPrice { get; set; }

    [Display(Name = "Дата траты")]
    public DateOnly? SpentAt { get; set; }

    [Display(Name = "Описание")]
    public string? Description { get; set; }
}
