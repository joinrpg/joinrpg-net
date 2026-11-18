using Bunit;
using JoinRpg.Web.Finances.Expenses;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Finances.Test;

public class ExpenseStateBadgeTest : BunitContext
{
    [Theory]
    [InlineData(ExpenseStatusView.Requested, "Запрошен", "label label-default", "Расход на одобрении у ответственного мастера, фактически не сделан")]
    [InlineData(ExpenseStatusView.SpentUnapproved, "Потрачен без согласования", "label label-warning", "Расход был фактически сделан, но пока еще не одобрен ответственным мастером")]
    [InlineData(ExpenseStatusView.Planned, "Запланирован", "label label-info", "Расход спланирован и одобрен ответственным мастером")]
    [InlineData(ExpenseStatusView.Spent, "Потрачен", "label label-success", "Расход фактически сделан в пределах одобренной суммы")]
    [InlineData(ExpenseStatusView.Deleted, "Удален", "label label-default", "Расход фактически не был потрачен и отменен")]
    [InlineData(ExpenseStatusView.NotApproved, "Не согласован", "label label-danger", "Расход был фактически сделан, но в согласовании ответственным мастером отказано")]
    public void RendersDisplayNameWithStatusColor(ExpenseStatusView status, string displayName, string cssClass, string description)
    {
        var cut = Render<ExpenseStateBadge>(p => p.Add(x => x.Status, status));

        var badge = cut.Find("span.label");
        badge.TextContent.Trim().ShouldBe(displayName);
        badge.ClassName.ShouldBe(cssClass);
        cut.Find("span.join-tooltip").TextContent.Trim().ShouldBe(description);
    }
}
