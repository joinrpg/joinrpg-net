using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.DomainTypes.Finances;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel;

/// <summary>
/// Расход проекта (ADR030). План — всегда, факт — позже (оба Actual* null, пока расход только
/// спланирован). Согласование — состояние <see cref="State"/> плюс история в <see cref="ExpenseApproval"/>.
/// </summary>
public class Expense : IProjectEntity, ICreatedUpdatedTrackedForEntity, IValidatableObject
{
    [Key]
    public int ExpenseId { get; set; }

    int IOrderableEntity.Id => ExpenseId;

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project Project { get; set; }

    [Required]
    public string Name { get; set; }

    /// <summary>Описание (markdown).</summary>
    public MarkdownDbValue Description { get; set; } = new MarkdownDbValue();

    /// <summary>Мастер, понёсший расход (по умолчанию — автор, сменяется в форме).</summary>
    [ForeignKey(nameof(MasterId))]
    public virtual User Master { get; set; }
    public int MasterId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public virtual ExpenseCategory Category { get; set; }
    public int CategoryId { get; set; }

    /// <summary>Id всех категорий пути от корня до листа — снапшот на момент создания/смены категории,
    /// по нему грид фильтрует поддерево без рекурсивных запросов (паттерн Character.ParentCharacterGroupIds).</summary>
    [NotMapped]
    public int[] CategoryPathIds
    {
        get => CategoryPathImpl._parentCharacterGroupIds;
        set => CategoryPathImpl.ListIds = value.Select(v => v.ToString()).JoinStrings(",");
    }

    public IntList CategoryPathImpl { get; set; } = new IntList();

    public int PlannedQuantity { get; set; }

    /// <summary>Цена за единицу в рублях (int), как в FinanceOperation.MoneyAmount.</summary>
    public int PlannedPrice { get; set; }

    public int? ActualQuantity { get; set; }

    public int? ActualPrice { get; set; }

    /// <summary>Дата (без времени), когда деньги реально потрачены; вводится при заполнении факта.
    /// Хранилище — datetime: EF6 не умеет DateOnly, доменный тип — <see cref="DateOnly"/>.</summary>
    public DateTime? SpentAt { get; set; }

    public ExpenseState State { get; set; }

    public DateTime CreatedAt { get; set; }
    [ForeignKey(nameof(CreatedById))]
    public virtual User CreatedBy { get; set; }
    public int CreatedById { get; set; }

    public DateTime UpdatedAt { get; set; }
    [ForeignKey(nameof(UpdatedById))]
    public virtual User UpdatedBy { get; set; }
    public int UpdatedById { get; set; }

    #region Implementation of IValidatableObject

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("Expense should have a name", [nameof(Name)]);
        }
        if (PlannedQuantity <= 0)
        {
            yield return new ValidationResult("Planned quantity should be positive", [nameof(PlannedQuantity)]);
        }
        if (PlannedPrice <= 0)
        {
            yield return new ValidationResult("Planned price should be positive", [nameof(PlannedPrice)]);
        }
        if ((ActualQuantity is null) != (ActualPrice is null))
        {
            yield return new ValidationResult("Actual quantity and actual price should be filled together",
                [nameof(ActualQuantity), nameof(ActualPrice)]);
        }
        if (ActualQuantity is <= 0)
        {
            yield return new ValidationResult("Actual quantity should be positive", [nameof(ActualQuantity)]);
        }
        if (ActualPrice is <= 0)
        {
            yield return new ValidationResult("Actual price should be positive", [nameof(ActualPrice)]);
        }
        if (SpentAt is not null && ActualQuantity is null)
        {
            yield return new ValidationResult("Spent date should be set only when actual is filled", [nameof(SpentAt)]);
        }
    }

    #endregion

    Project IProjectEntity.Project => Project;
}
