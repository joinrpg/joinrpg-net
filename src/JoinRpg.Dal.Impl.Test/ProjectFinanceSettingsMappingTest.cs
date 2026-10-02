using JoinRpg.DataModel.Mocks;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

/// <summary>
/// Маппинг глобальных финансовых настроек проекта в <c>ProjectInfo</c>. БД не используется —
/// только in-memory граф <c>Project</c> из мока.
/// </summary>
public class ProjectFinanceSettingsMappingTest
{
    private readonly MockedProject mock = new();

    [Fact]
    public void WarnOnOverPayment_IsEnabledByDefault()
        => mock.ProjectInfo.ProjectFinanceSettings.WarnOnOverPayment.ShouldBeTrue();

    [Fact]
    public void WarnOnOverPayment_IsMappedFromProjectDetails()
    {
        mock.Project.Details.FinanceWarnOnOverPayment = false;

        mock.ReInitProjectInfo();

        mock.ProjectInfo.ProjectFinanceSettings.WarnOnOverPayment.ShouldBeFalse();
    }

    [Fact]
    public void PreferentialFeeEnabled_IsMappedFromProjectDetails()
    {
        mock.Project.Details.PreferentialFeeEnabled = true;

        mock.ReInitProjectInfo();

        mock.ProjectInfo.ProjectFinanceSettings.PreferentialFeeEnabled.ShouldBeTrue();
    }
}
