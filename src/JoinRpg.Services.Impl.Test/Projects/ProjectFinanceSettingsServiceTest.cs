using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Impl.Projects.Metadata;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Test.Projects;

public class ProjectFinanceSettingsServiceTest : ProjectMetadataServiceTestBase
{
    private readonly FakeVirtualUsersService vpu = new();

    private ProjectFinanceSettingsService CreateService(int? currentUserId = null, bool isAdmin = false)
        => new(CreatePropsService(CreateCurrentUser(currentUserId, isAdmin)), vpu);

    private ProjectFinanceSettings FinanceSettings => Result.ProjectFinanceSettings;

    /// <summary>Добавляет тип оплаты прямо в мок, минуя сервис (заготовка состояния для теста).</summary>
    private PaymentType AddPaymentType(PaymentTypeKind kind, bool isActive = true, int? userId = null)
    {
        var paymentType = new PaymentType(kind, mock.Project.ProjectId, userId ?? mock.Master.UserId)
        {
            PaymentTypeId = mock.Project.PaymentTypes.Count + 1,
            IsActive = isActive,
        };
        mock.Project.PaymentTypes.Add(paymentType);
        mock.ReInitProjectInfo();
        return paymentType;
    }

    private PaymentTypeIdentification PaymentTypeId(PaymentType paymentType)
        => new(ProjectId, paymentType.PaymentTypeId);

    /// <summary>Добавляет строку расписания взносов прямо в мок, минуя сервис.</summary>
    private ProjectFeeSetting AddFeeSetting(DateTime startDate)
    {
        var feeSetting = new ProjectFeeSetting
        {
            ProjectFeeSettingId = mock.Project.ProjectFeeSettings.Count + 1,
            ProjectId = mock.Project.ProjectId,
            Fee = 1000,
            StartDate = startDate,
        };
        mock.Project.ProjectFeeSettings.Add(feeSetting);
        mock.ReInitProjectInfo();
        return feeSetting;
    }

    private SetFinanceSettingsRequest GlobalSettings(bool preferentialFeeEnabled) => new()
    {
        ProjectId = ProjectId,
        WarnOnOverPayment = true,
        PreferentialFeeEnabled = preferentialFeeEnabled,
        PreferentialFeeConditions = "Условия",
    };

    #region Общие настройки

    [Fact]
    public async Task SaveGlobalSettingsShouldAppearInProjectInfo()
    {
        await CreateService().SaveGlobalSettings(GlobalSettings(preferentialFeeEnabled: true));

        FinanceSettings.PreferentialFeeEnabled.ShouldBeTrue();
        mock.Project.Details.FinanceWarnOnOverPayment.ShouldBeTrue();
        mock.Project.Details.PreferentialFeeConditions.Contents.ShouldBe("Условия");
    }

    [Fact]
    public async Task CantChangeSettingsWithoutManageMoneyPermission()
        => _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(currentUserId: mock.Player.UserId).SaveGlobalSettings(GlobalSettings(preferentialFeeEnabled: true)));

    [Fact]
    public async Task CantChangeSettingsInArchivedProject()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().SaveGlobalSettings(GlobalSettings(preferentialFeeEnabled: true)));
    }

    #endregion

    #region Расписание взносов

    [Fact]
    public async Task CreateFeeSettingShouldAppearInProjectInfo()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(10);

        await CreateService().CreateFeeSetting(new CreateFeeSettingRequest
        {
            ProjectId = ProjectId,
            Fee = 500,
            PreferentialFee = null,
            StartDate = startDate,
        });

        var fee = FinanceSettings.FeeSchedule.ShouldHaveSingleItem();
        fee.Fee.ShouldBe(500);
        // Самая ранняя строка расписания всегда сдвигается к дате создания проекта.
        fee.StartDate.ShouldBe(mock.Project.CreatedDate);
    }

    [Fact]
    public async Task SecondFeeSettingKeepsItsOwnStartDate()
    {
        var service = CreateService();
        var startDate = DateTime.UtcNow.Date.AddDays(10);

        await service.CreateFeeSetting(new CreateFeeSettingRequest { ProjectId = ProjectId, Fee = 500, PreferentialFee = null, StartDate = DateTime.UtcNow.Date });
        await service.CreateFeeSetting(new CreateFeeSettingRequest { ProjectId = ProjectId, Fee = 700, PreferentialFee = null, StartDate = startDate });

        FinanceSettings.FeeSchedule.Count.ShouldBe(2);
        FinanceSettings.GetFeeForDate(startDate, preferential: false).ShouldBe(700);
    }

    [Fact]
    public async Task CantCreateFeeSettingInPast()
        => _ = await Should.ThrowAsync<CannotPerformOperationInPast>(
            () => CreateService().CreateFeeSetting(new CreateFeeSettingRequest
            {
                ProjectId = ProjectId,
                Fee = 500,
                PreferentialFee = null,
                StartDate = DateTime.UtcNow.Date.AddDays(-10),
            }));

    [Fact]
    public async Task CantSetPreferentialFeeWhenItIsDisabled()
        => _ = await Should.ThrowAsync<PreferentialFeeNotEnabled>(
            () => CreateService().CreateFeeSetting(new CreateFeeSettingRequest
            {
                ProjectId = ProjectId,
                Fee = 500,
                PreferentialFee = 100,
                StartDate = DateTime.UtcNow.Date.AddDays(10),
            }));

    [Fact]
    public async Task PreferentialFeeAllowedWhenEnabled()
    {
        var service = CreateService();
        await service.SaveGlobalSettings(GlobalSettings(preferentialFeeEnabled: true));

        await service.CreateFeeSetting(new CreateFeeSettingRequest
        {
            ProjectId = ProjectId,
            Fee = 500,
            PreferentialFee = 100,
            StartDate = DateTime.UtcNow.Date.AddDays(10),
        });

        FinanceSettings.FeeSchedule.ShouldHaveSingleItem().PreferentialFee.ShouldBe(100);
    }

    [Fact]
    public async Task DeleteFeeSettingShouldDisappearFromProjectInfo()
    {
        var feeSetting = AddFeeSetting(DateTime.UtcNow.Date.AddDays(10));

        await CreateService().DeleteFeeSetting(ProjectId, feeSetting.ProjectFeeSettingId);

        FinanceSettings.FeeSchedule.ShouldBeEmpty();
    }

    [Fact]
    public async Task CantDeleteFeeSettingFromPast()
    {
        var feeSetting = AddFeeSetting(DateTime.UtcNow.Date.AddDays(-10));

        _ = await Should.ThrowAsync<CannotPerformOperationInPast>(
            () => CreateService().DeleteFeeSetting(ProjectId, feeSetting.ProjectFeeSettingId));
    }

    #endregion

    #region Типы оплаты

    [Fact]
    public async Task CreateCashPaymentTypeShouldAppearInProjectInfo()
    {
        await CreateService().CreatePaymentType(new CreatePaymentTypeRequest
        {
            ProjectId = ProjectId,
            TargetMasterId = new UserIdentification(mock.Master.UserId),
            TypeKind = PaymentTypeKind.Cash,
            Name = null,
        });

        var paymentType = FinanceSettings.PaymentTypes.ShouldHaveSingleItem();
        paymentType.TypeKind.ShouldBe(PaymentTypeKind.Cash);
        paymentType.Enabled.ShouldBeTrue();
        paymentType.User.UserId.ShouldBe(new UserIdentification(mock.Master.UserId));
    }

    [Fact]
    public async Task CantCreateSecondCashPaymentTypeForSameMaster()
    {
        _ = AddPaymentType(PaymentTypeKind.Cash);

        _ = await Should.ThrowAsync<JoinRpgInvalidUserException>(
            () => CreateService().CreatePaymentType(new CreatePaymentTypeRequest
            {
                ProjectId = ProjectId,
                TargetMasterId = new UserIdentification(mock.Master.UserId),
                TypeKind = PaymentTypeKind.Cash,
                Name = null,
            }));
    }

    [Fact]
    public async Task CantCreatePaymentTypeForNonMaster()
        => _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService().CreatePaymentType(new CreatePaymentTypeRequest
            {
                ProjectId = ProjectId,
                TargetMasterId = new UserIdentification(mock.Player.UserId),
                TypeKind = PaymentTypeKind.Cash,
                Name = null,
            }));

    private void BlockProject()
    {
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();
    }

    // ADR023: типы оплаты и сетку взносов после мая мастер заводит заново до снятия блокировки.
    [Fact]
    public async Task PaymentTypeSettings_InBlockedProject_Succeed()
    {
        var existing = AddPaymentType(PaymentTypeKind.Custom);
        BlockProject();

        await CreateService().CreatePaymentType(new CreatePaymentTypeRequest
        {
            ProjectId = ProjectId,
            TargetMasterId = new UserIdentification(mock.Master.UserId),
            TypeKind = PaymentTypeKind.Custom,
            Name = "Перевод на карту",
        });
        await CreateService().EditCustomPaymentType(PaymentTypeId(existing), "Наличные казначею", isDefault: true);
        await CreateService().TogglePaymentActiveness(PaymentTypeId(existing));

        FinanceSettings.PaymentTypes.Select(pt => pt.Name).ShouldBe(["Перевод на карту", "Наличные казначею"], ignoreOrder: true);
        existing.IsActive.ShouldBeFalse();
        unitOfWork.SaveChangesCallCount.ShouldBe(3);
    }

    [Fact]
    public async Task FeeAndGlobalSettings_InBlockedProject_Succeed()
    {
        var removed = AddFeeSetting(DateTime.UtcNow.Date.AddDays(20));
        BlockProject();

        await CreateService().SaveGlobalSettings(GlobalSettings(preferentialFeeEnabled: true));
        await CreateService().CreateFeeSetting(new CreateFeeSettingRequest
        {
            ProjectId = ProjectId,
            Fee = 500,
            PreferentialFee = 300,
            StartDate = DateTime.UtcNow.Date.AddDays(10),
        });
        await CreateService().DeleteFeeSetting(ProjectId, removed.ProjectFeeSettingId);

        FinanceSettings.PreferentialFeeEnabled.ShouldBeTrue();
        var fee = FinanceSettings.FeeSchedule.ShouldHaveSingleItem();
        fee.Fee.ShouldBe(500);
        fee.StartDate.ShouldBe(mock.Project.CreatedDate);
        unitOfWork.SaveChangesCallCount.ShouldBe(3);
    }

    [Fact]
    public async Task CantCreatePaymentTypeInArchivedProject()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().CreatePaymentType(new CreatePaymentTypeRequest
            {
                ProjectId = ProjectId,
                TargetMasterId = new UserIdentification(mock.Master.UserId),
                TypeKind = PaymentTypeKind.Cash,
                Name = null,
            }));

        mock.Project.PaymentTypes.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateRestoredCashPaymentType_InBlockedProject_CreatesCashForOwner()
    {
        BlockProject();

        var result = await CreateService().CreateRestoredCashPaymentType(ProjectId, new UserIdentification(mock.Master.UserId));

        var paymentType = FinanceSettings.PaymentTypes.ShouldHaveSingleItem();
        paymentType.TypeKind.ShouldBe(PaymentTypeKind.Cash);
        paymentType.Enabled.ShouldBeTrue();
        paymentType.User.UserId.ShouldBe(new UserIdentification(mock.Master.UserId));
        result.ShouldBe(paymentType.PaymentTypeId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateRestoredCashPaymentType_ExistingCash_IsReturnedEvenIfDisabled(bool isActive)
    {
        var existing = AddPaymentType(PaymentTypeKind.Cash, isActive);
        BlockProject();

        var result = await CreateService().CreateRestoredCashPaymentType(ProjectId, new UserIdentification(mock.Master.UserId));

        result.ShouldBe(PaymentTypeId(existing));
        mock.Project.PaymentTypes.ShouldHaveSingleItem().IsActive.ShouldBe(isActive);
    }

    [Fact]
    public async Task CreateRestoredCashPaymentType_ActiveProject_Throws()
    {
        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().CreateRestoredCashPaymentType(ProjectId, new UserIdentification(mock.Master.UserId)));

        mock.Project.PaymentTypes.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateRestoredCashPaymentType_OwnerNotAMaster_Throws()
    {
        BlockProject();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService().CreateRestoredCashPaymentType(ProjectId, new UserIdentification(mock.Player.UserId)));

        mock.Project.PaymentTypes.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateRestoredCashPaymentType_WithoutManageMoneyPermission_Throws()
    {
        var restorer = mock.CreateMaster("Мастер без денег");
        mock.Project.ProjectAcls.Single(a => a.UserId == restorer.UserId).CanManageMoney = false;
        BlockProject();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(currentUserId: restorer.UserId).CreateRestoredCashPaymentType(ProjectId, new UserIdentification(mock.Master.UserId)));

        mock.Project.PaymentTypes.ShouldBeEmpty();
    }

    [Fact]
    public async Task EditCustomPaymentTypeShouldAppearInProjectInfo()
    {
        var paymentType = AddPaymentType(PaymentTypeKind.Custom);

        await CreateService().EditCustomPaymentType(PaymentTypeId(paymentType), "Перевод на карту", isDefault: true);

        var result = FinanceSettings.PaymentTypes.ShouldHaveSingleItem();
        result.Name.ShouldBe("Перевод на карту");
        result.IsDefault.ShouldBeTrue();
    }

    /// <summary>
    /// Выключение типа оплаты — всегда soft-delete: физически он не удаляется, иначе потерялась бы
    /// связь с уже проведёнными операциями (см. PaymentType.CanBePermanentlyDeleted).
    /// </summary>
    [Fact]
    public async Task DisablingPaymentTypeIsSoftDelete()
    {
        var paymentType = AddPaymentType(PaymentTypeKind.Custom);

        await CreateService().TogglePaymentActiveness(PaymentTypeId(paymentType));

        paymentType.IsActive.ShouldBeFalse();
        mock.Project.PaymentTypes.ShouldContain(paymentType);
        FinanceSettings.PaymentTypes.ShouldHaveSingleItem().Enabled.ShouldBeFalse();
    }

    [Fact]
    public async Task MasterCanDisableOnlinePaymentType()
    {
        var paymentType = AddPaymentType(PaymentTypeKind.Online, userId: vpu.PaymentsUser.UserId);

        await CreateService().TogglePaymentActiveness(PaymentTypeId(paymentType));

        FinanceSettings.PaymentTypes.ShouldHaveSingleItem().Enabled.ShouldBeFalse();
    }

    [Fact]
    public async Task MasterCantEnableOnlinePaymentTypeBack()
    {
        var paymentType = AddPaymentType(PaymentTypeKind.Online, isActive: false, userId: vpu.PaymentsUser.UserId);

        _ = await Should.ThrowAsync<MustBeAdminException>(
            () => CreateService().TogglePaymentActiveness(PaymentTypeId(paymentType)));
    }

    [Fact]
    public async Task AdminCanEnableOnlinePaymentTypeBack()
    {
        var paymentType = AddPaymentType(PaymentTypeKind.Online, isActive: false, userId: vpu.PaymentsUser.UserId);

        await CreateService(currentUserId: mock.Player.UserId, isAdmin: true)
            .TogglePaymentActiveness(PaymentTypeId(paymentType));

        FinanceSettings.PaymentTypes.ShouldHaveSingleItem().Enabled.ShouldBeTrue();
    }

    [Fact]
    public async Task CantTogglePaymentTypeWithoutManageMoneyPermission()
    {
        var paymentType = AddPaymentType(PaymentTypeKind.Custom);

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(currentUserId: mock.Player.UserId)
                .TogglePaymentActiveness(PaymentTypeId(paymentType)));
    }

    #endregion
}
