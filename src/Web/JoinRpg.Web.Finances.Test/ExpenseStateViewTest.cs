using JoinRpg.DomainTypes.Finances;
using JoinRpg.Helpers;
using JoinRpg.Web.Finances.Expenses;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Finances.Test;

public class ExpenseStateViewTest
{
    [Theory]
    [InlineData(ExpenseState.Proposed, ExpenseStateView.Proposed)]
    [InlineData(ExpenseState.Approved, ExpenseStateView.Approved)]
    [InlineData(ExpenseState.Rejected, ExpenseStateView.Rejected)]
    public void MapsDomainStateToView(ExpenseState domainState, ExpenseStateView expected)
        => domainState.ToView().ShouldBe(expected);

    [Theory]
    [InlineData(ExpenseStateView.Proposed, "На рассмотрении")]
    [InlineData(ExpenseStateView.Approved, "Одобрен")]
    [InlineData(ExpenseStateView.Rejected, "Отклонён")]
    public void DisplayNamesAreRussian(ExpenseStateView state, string expectedName)
        => state.GetDisplayName().ShouldBe(expectedName);
}
