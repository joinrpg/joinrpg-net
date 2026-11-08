using System.ComponentModel.DataAnnotations;
using JoinRpg.DomainTypes.Finances;

namespace JoinRpg.Web.Finances.Expenses;

public enum ExpenseStateView
{
    [Display(Name = "На рассмотрении")]
    Proposed,

    [Display(Name = "Одобрен")]
    Approved,

    [Display(Name = "Отклонён")]
    Rejected,
}

public static class ExpenseStateViewExtensions
{
    public static ExpenseStateView ToView(this ExpenseState state) => state switch
    {
        ExpenseState.Proposed => ExpenseStateView.Proposed,
        ExpenseState.Approved => ExpenseStateView.Approved,
        ExpenseState.Rejected => ExpenseStateView.Rejected,
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };
}
