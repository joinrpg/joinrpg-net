using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Finances.Expenses;

/// <summary>
/// Комбинированный статус расхода для грида и формы: состояние согласования + фактически ли сделана трата.
/// </summary>
public enum ExpenseStatusView
{
    [Display(Name = "Запрошен", Description = "Расход на одобрении у ответственного мастера, фактически не сделан")]
    Requested,

    [Display(Name = "Потрачен без согласования", Description = "Расход был фактически сделан, но пока еще не одобрен ответственным мастером")]
    SpentUnapproved,

    [Display(Name = "Запланирован", Description = "Расход спланирован и одобрен ответственным мастером")]
    Planned,

    [Display(Name = "Потрачен", Description = "Расход фактически сделан в пределах одобренной суммы")]
    Spent,

    [Display(Name = "Удален", Description = "Расход фактически не был потрачен и отменен")]
    Deleted,

    [Display(Name = "Не согласован", Description = "Расход был фактически сделан, но в согласовании ответственным мастером отказано")]
    NotApproved,
}

public static class ExpenseStatusViewExtensions
{
    /// <summary>Статус для грида: согласование расхода + признак фактической траты.</summary>
    public static ExpenseStatusView ToStatus(this ExpenseStateView state, bool hasActualCost) => (state, hasActualCost) switch
    {
        (ExpenseStateView.Proposed, false) => ExpenseStatusView.Requested,
        (ExpenseStateView.Proposed, true) => ExpenseStatusView.SpentUnapproved,
        (ExpenseStateView.Approved, false) => ExpenseStatusView.Planned,
        (ExpenseStateView.Approved, true) => ExpenseStatusView.Spent,
        (ExpenseStateView.Rejected, false) => ExpenseStatusView.Deleted,
        (ExpenseStateView.Rejected, true) => ExpenseStatusView.NotApproved,
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };
}
