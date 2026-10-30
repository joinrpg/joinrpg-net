using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Interfaces;
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
    private readonly RecordingFinanceSettingsService financeSettingsService = new();

    // Возвращаем интерфейс намеренно: методы сервиса — явная реализация IFinanceSettingsClient.
#pragma warning disable CA1859
    private IFinanceSettingsClient CreateService(int userId, bool isAdmin = false) =>
#pragma warning restore CA1859
        new FinanceSettingsViewService(
            new FakeProjectMetadataRepository(Mock),
            financeSettingsService,
            new FakeCurrentUserAccessor(new UserIdentification(userId), isAdmin),
            new FakeVirtualUsersService());

    private const int PaymentsUserId = 9000;

    private PaymentType AddPaymentType(PaymentTypeKind kind, int userId, bool isActive = true, string? name = null, bool isDefault = false)
    {
        var paymentType = new PaymentType(kind, Mock.Project.ProjectId, userId)
        {
            PaymentTypeId = Mock.Project.PaymentTypes.Count + 1,
            IsActive = isActive,
            IsDefault = isDefault,
            Name = name,
        };
        Mock.Project.PaymentTypes.Add(paymentType);
        Mock.ReInitProjectInfo();
        return paymentType;
    }

    [Fact]
    public async Task OnlineFirstThenActiveDefaultCustomCashAndInactive()
    {
        var master2 = Mock.CreateMaster();
        AddPaymentType(PaymentTypeKind.Online, PaymentsUserId);
        AddPaymentType(PaymentTypeKind.Custom, Mock.Master.UserId, name: "Карта");
        AddPaymentType(PaymentTypeKind.Custom, Mock.Master.UserId, name: "Банк", isDefault: true);
        AddPaymentType(PaymentTypeKind.Cash, Mock.Master.UserId);
        AddPaymentType(PaymentTypeKind.Custom, Mock.Master.UserId, isActive: false, name: "Старый счёт");

        var items = (await CreateService(Mock.Master.UserId).GetPaymentTypes(Mock.ProjectInfo.ProjectId)).PaymentTypes;

        items.Select(i => (i.Kind, i.Name, i.IsActive, i.PaymentTypeId is not null)).ShouldBe([
            (PaymentTypeKind.Online, "Онлайн", true, true),
            // Подписку ещё не включали — строка без id
            (PaymentTypeKind.OnlineSubscription, "Подписка", false, false),
            (PaymentTypeKind.Custom, "Банк", true, true),
            (PaymentTypeKind.Custom, "Карта", true, true),
            (PaymentTypeKind.Cash, "Наличными", true, true),
            (PaymentTypeKind.Custom, "Старый счёт", false, true),
            (PaymentTypeKind.Cash, "Наличными", false, false),
        ]);
        // Наличные ещё не включены у второго мастера — строка для него, без id
        items[6].Master.UserId.ShouldBe(new UserIdentification(master2.UserId));
        items[2].IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task OnlyAdminCanToggleOnlinePayments()
    {
        AddPaymentType(PaymentTypeKind.Custom, Mock.Master.UserId, name: "Карта");

        var master = (await CreateService(Mock.Master.UserId).GetPaymentTypes(Mock.ProjectInfo.ProjectId)).PaymentTypes;
        var admin = (await CreateService(Mock.Master.UserId, isAdmin: true).GetPaymentTypes(Mock.ProjectInfo.ProjectId)).PaymentTypes;

        master.Where(i => i.IsOnline).ShouldAllBe(i => !i.CanToggle);
        master.Where(i => !i.IsOnline).ShouldAllBe(i => i.CanToggle);
        admin.ShouldAllBe(i => i.CanToggle);
    }

    [Theory]
    [InlineData(PaymentTypeKind.Custom, 2, "Карта", "Карта")]
    // Своё имя только у произвольного метода — у остальных его не передаём, даже если пришло
    [InlineData(PaymentTypeKind.Cash, 2, "Карта", null)]
    [InlineData(PaymentTypeKind.Online, null, null, null)]
    public async Task CreatePaymentTypePassesKindMasterAndOnlyCustomName(PaymentTypeKind kind, int? masterId, string? name, string? expectedName)
    {
        var master = masterId is { } id ? new UserIdentification(id) : null;

        await CreateService(Mock.Master.UserId).CreatePaymentType(Mock.ProjectInfo.ProjectId, new NewPaymentTypeViewModel(kind, master, name));

        var request = financeSettingsService.CreatedPaymentType.ShouldNotBeNull();
        request.ProjectId.ShouldBe(Mock.ProjectInfo.ProjectId);
        request.TypeKind.ShouldBe(kind);
        request.TargetMasterId.ShouldBe(master);
        request.Name.ShouldBe(expectedName);
    }

    [Fact]
    public async Task FeeWithoutActivePaymentTypesIsReported()
    {
        Mock.Project.ProjectFeeSettings.Add(new ProjectFeeSetting { ProjectFeeSettingId = 1, StartDate = DateTime.UtcNow.AddDays(-10), Fee = 1000 });
        Mock.ReInitProjectInfo();

        var model = await CreateService(Mock.Master.UserId).GetPaymentTypes(Mock.ProjectInfo.ProjectId);

        model.HasFeeSettings.ShouldBeTrue();
        model.PaymentTypes.ShouldAllBe(i => !i.IsActive);
    }

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

    private sealed class FakeVirtualUsersService : IVirtualUsersService
    {
        public User PaymentsUser { get; } = new() { UserId = PaymentsUserId, PrefferedName = "Платёжная система", Email = "payments@example.com", Claims = [] };
        public User RobotUser => throw new NotSupportedException();
        public UserIdentification RobotUserId => throw new NotSupportedException();
    }

    /// <summary>Запоминает создание метода оплаты; остальное тестам не нужно.</summary>
    private sealed class RecordingFinanceSettingsService : IProjectFinanceSettingsService
    {
        public CreatePaymentTypeRequest? CreatedPaymentType { get; private set; }

        public Task CreatePaymentType(CreatePaymentTypeRequest request)
        {
            CreatedPaymentType = request;
            return Task.CompletedTask;
        }

        public Task<PaymentTypeIdentification> CreateRestoredCashPaymentType(ProjectIdentification projectId, UserIdentification owner) => throw new NotSupportedException();
        public Task TogglePaymentActiveness(PaymentTypeIdentification paymentTypeId) => throw new NotSupportedException();
        public Task EditCustomPaymentType(PaymentTypeIdentification paymentTypeId, string name, bool isDefault) => throw new NotSupportedException();
        public Task CreateFeeSetting(CreateFeeSettingRequest request) => throw new NotSupportedException();
        public Task DeleteFeeSetting(ProjectIdentification projectId, int projectFeeSettingId) => throw new NotSupportedException();
        public Task SaveGlobalSettings(SetFinanceSettingsRequest request) => throw new NotSupportedException();
    }
}
