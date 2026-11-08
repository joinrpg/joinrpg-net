using Bunit;
using JoinRpg.Web.Finances.Expenses;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Finances.Test;

public class ExpenseStateBadgeTest : BunitContext
{
    [Theory]
    [InlineData(ExpenseStateView.Proposed, "На рассмотрении", "label label-default")]
    [InlineData(ExpenseStateView.Approved, "Одобрен", "label label-success")]
    [InlineData(ExpenseStateView.Rejected, "Отклонён", "label label-danger")]
    public void RendersDisplayNameWithStateColor(ExpenseStateView state, string displayName, string cssClass)
    {
        var cut = Render<ExpenseStateBadge>(p => p.Add(x => x.State, state));

        var badge = cut.Find("span.label");
        badge.TextContent.Trim().ShouldBe(displayName);
        badge.ClassName.ShouldBe(cssClass);
    }
}
