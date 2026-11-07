using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel;

/// <summary>
/// Категория расхода (ADR030) — узел дерева на проект. Путь от корня денормализован в
/// <see cref="ParentCategoriesImpl"/> (паттерн CharacterGroup.ParentGroupsImpl). Явных операций
/// удаления на первом этапе нет — IsActive всегда true.
/// </summary>
public class ExpenseCategory : IProjectEntity, IDeletableSubEntity
{
    [Key]
    public int ExpenseCategoryId { get; set; }

    int IOrderableEntity.Id => ExpenseCategoryId;

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project Project { get; set; }

    [Required]
    public string Name { get; set; }

    public int? ParentCategoryId { get; set; }

    [ForeignKey(nameof(ParentCategoryId))]
    public virtual ExpenseCategory? ParentCategory { get; set; }

    /// <summary>Id категорий от корня до родителя включительно — снапшот пути (паттерн CharacterGroup.ParentGroupsImpl).</summary>
    [NotMapped]
    public int[] ParentCategoryIds
    {
        get => ParentCategoriesImpl._parentCharacterGroupIds;
        set => ParentCategoriesImpl.ListIds = value.Select(v => v.ToString()).JoinStrings(",");
    }

    public IntList ParentCategoriesImpl { get; set; } = new IntList();

    public bool IsActive { get; set; } = true;

    public bool CanBePermanentlyDeleted => false;

    Project IProjectEntity.Project => Project;
}
