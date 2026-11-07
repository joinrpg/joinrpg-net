using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.DomainTypes.Finances;

namespace JoinRpg.DataModel;

/// <summary>
/// Запись истории согласования расхода (ADR030): состояние, в которое перевели, кто и когда.
/// Живёт и удаляется вместе с расходом.
/// </summary>
public class ExpenseApproval
{
    [Key]
    public int ExpenseApprovalId { get; set; }

    public int ExpenseId { get; set; }

    [ForeignKey(nameof(ExpenseId))]
    public virtual Expense Expense { get; set; }

    /// <summary>Состояние, в которое перевели.</summary>
    public ExpenseState State { get; set; }

    [ForeignKey(nameof(ApprovedById))]
    public virtual User ApprovedBy { get; set; }
    public int ApprovedById { get; set; }

    public DateTime Timestamp { get; set; }

    /// <summary>Комментарий к решению (markdown).</summary>
    public MarkdownDbValue Comment { get; set; } = new MarkdownDbValue();
}
