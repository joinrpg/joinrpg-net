using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.ProjectMasterTools.Finance;
using JoinRpg.WebPortal.Managers.Projects;

namespace JoinRpg.WebPortal.Managers.Test.Projects;

/// <summary>
/// <see cref="FinanceSettingsViewService"/> — данные панелей страницы «Настройка финансов».
/// </summary>
public class FinanceSettingsViewServiceTests
{
    private MockedProject Mock { get; } = new MockedProject();

    // Возвращаем интерфейс намеренно: методы сервиса — явная реализация IFinanceSettingsClient.
#pragma warning disable CA1859
    private IFinanceSettingsClient CreateService(int userId) =>
#pragma warning restore CA1859
        new FinanceSettingsViewService(
            new FakeProjectMetadataRepository(Mock),
            new ThrowingFinanceSettingsService(),
            new FakeCurrentUserAccessor(new UserIdentification(userId)));

    [Fact]
    public async Task OnlyFeeNotInForceYetCanBeDeleted()
    {
        Mock.Project.ProjectFeeSettings.Add(new ProjectFeeSetting { ProjectFeeSettingId = 1, StartDate = DateTime.UtcNow.AddDays(-10), Fee = 1000 });
        Mock.Project.ProjectFeeSettings.Add(new ProjectFeeSetting { ProjectFeeSettingId = 2, StartDate = DateTime.UtcNow.AddDays(10), Fee = 1500, PreferentialFee = 700 });
        Mock.ReInitProjectInfo();

        var fees = (await CreateService(Mock.Master.UserId).GetFeeSettings(Mock.ProjectInfo.ProjectId)).Fees;

        fees.Select(f => (f.ProjectFeeSettingId, f.Fee, f.PreferentialFee, f.CanDelete))
            .ShouldBe([(1, 1000, null, false), (2, 1500, 700, true)]);
    }

    [Fact]
    public async Task MasterWithoutMoneyPermissionGetsReadOnlyPanels()
    {
        var master = Mock.CreateMaster();
        Mock.Project.ProjectAcls.Single(acl => acl.UserId == master.UserId).CanManageMoney = false;
        Mock.ReInitProjectInfo();
        var service = CreateService(master.UserId);

        (await service.GetGeneralSettings(Mock.ProjectInfo.ProjectId)).HasEditAccess.ShouldBeFalse();
        (await service.GetFeeSettings(Mock.ProjectInfo.ProjectId)).HasEditAccess.ShouldBeFalse();
    }

    [Fact]
    public async Task MasterWithMoneyPermissionCanEdit()
    {
        var service = CreateService(Mock.Master.UserId);

        (await service.GetGeneralSettings(Mock.ProjectInfo.ProjectId)).HasEditAccess.ShouldBeTrue();
        (await service.GetFeeSettings(Mock.ProjectInfo.ProjectId)).HasEditAccess.ShouldBeTrue();
    }

    /// <summary>Тесты чтения не должны ничего менять.</summary>
    private sealed class ThrowingFinanceSettingsService : IProjectFinanceSettingsService
    {
        public Task CreatePaymentType(CreatePaymentTypeRequest request) => throw new NotSupportedException();
        public Task<PaymentTypeIdentification> CreateRestoredCashPaymentType(ProjectIdentification projectId, UserIdentification owner) => throw new NotSupportedException();
        public Task TogglePaymentActiveness(PaymentTypeIdentification paymentTypeId) => throw new NotSupportedException();
        public Task EditCustomPaymentType(PaymentTypeIdentification paymentTypeId, string name, bool isDefault) => throw new NotSupportedException();
        public Task CreateFeeSetting(CreateFeeSettingRequest request) => throw new NotSupportedException();
        public Task DeleteFeeSetting(ProjectIdentification projectId, int projectFeeSettingId) => throw new NotSupportedException();
        public Task SaveGlobalSettings(SetFinanceSettingsRequest request) => throw new NotSupportedException();
    }
}
