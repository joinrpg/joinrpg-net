using JoinRpg.Helpers;
using JoinRpg.Web.Finances.Expenses;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Finances.Test;

public class ExpenseStatusViewTest
{
    [Theory]
    [InlineData(ExpenseStateView.Proposed, false, ExpenseStatusView.Requested)]
    [InlineData(ExpenseStateView.Proposed, true, ExpenseStatusView.SpentUnapproved)]
    [InlineData(ExpenseStateView.Approved, false, ExpenseStatusView.Planned)]
    [InlineData(ExpenseStateView.Approved, true, ExpenseStatusView.Spent)]
    [InlineData(ExpenseStateView.Rejected, false, ExpenseStatusView.Deleted)]
    [InlineData(ExpenseStateView.Rejected, true, ExpenseStatusView.NotApproved)]
    public void MapsStateAndFactToStatus(ExpenseStateView state, bool hasActualCost, ExpenseStatusView expected)
        => state.ToStatus(hasActualCost).ShouldBe(expected);

    [Theory]
    [InlineData(ExpenseStatusView.Requested, "Запрошен")]
    [InlineData(ExpenseStatusView.SpentUnapproved, "Потрачен без согласования")]
    [InlineData(ExpenseStatusView.Planned, "Запланирован")]
    [InlineData(ExpenseStatusView.Spent, "Потрачен")]
    [InlineData(ExpenseStatusView.Deleted, "Удален")]
    [InlineData(ExpenseStatusView.NotApproved, "Не согласован")]
    public void DisplayNamesAreRussian(ExpenseStatusView status, string expectedName)
        => status.GetDisplayName().ShouldBe(expectedName);

    [Theory]
    [InlineData(ExpenseStatusView.Requested, "Расход на одобрении у ответственного мастера, фактически не сделан")]
    [InlineData(ExpenseStatusView.SpentUnapproved, "Расход был фактически сделан, но пока еще не одобрен ответственным мастером")]
    [InlineData(ExpenseStatusView.Planned, "Расход спланирован и одобрен ответственным мастером")]
    [InlineData(ExpenseStatusView.Spent, "Расход фактически сделан в пределах одобренной суммы")]
    [InlineData(ExpenseStatusView.Deleted, "Расход фактически не был потрачен и отменен")]
    [InlineData(ExpenseStatusView.NotApproved, "Расход был фактически сделан, но в согласовании ответственным мастером отказано")]
    public void DescriptionsAreRussian(ExpenseStatusView status, string expectedDescription)
        => status.GetDescription().ShouldBe(expectedDescription);
}
