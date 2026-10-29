using System.Data;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Portal.Controllers.WebApi;
using JoinRpg.Web.ProjectMasterTools.Finance;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Ручки панели «Взнос и сроки»: штатные отказы сервиса уходят острову текстом, который можно показать мастеру.
/// </summary>
public class FinanceSettingsControllerTest
{
    private static readonly ProjectIdentification ProjectId = new(1611);

    public static TheoryData<Exception, string> Refusals => new()
    {
        { new CannotPerformOperationInPast(), "задним числом" },
        { new PreferentialFeeNotEnabled(), "Льготный взнос в проекте выключен" },
        { new JoinRpgEntityNotFoundException(7, "ProjectFeeSetting"), "Взнос не найден" },
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public async Task CreateRefusalIsBadRequestWithReason(Exception refusal, string reason)
    {
        var controller = new FinanceSettingsController(new FakeClient { Failure = refusal });

        var result = await controller.CreateFeeSetting(ProjectId, new CreateFeeSettingViewModel { Fee = 1000, StartDate = new DateOnly(2030, 1, 1) });

        result.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBeOfType<string>().ShouldContain(reason);
    }

    [Theory]
    [MemberData(nameof(Refusals))]
    public async Task DeleteRefusalIsBadRequestWithReason(Exception refusal, string reason)
    {
        var controller = new FinanceSettingsController(new FakeClient { Failure = refusal });

        var result = await controller.DeleteFeeSetting(ProjectId, 7);

        result.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBeOfType<string>().ShouldContain(reason);
    }

    [Fact]
    public async Task DeleteGoesToAuthorizedProject()
    {
        var client = new FakeClient();
        var controller = new FinanceSettingsController(client);

        var result = await controller.DeleteFeeSetting(ProjectId, 7);

        _ = result.ShouldBeOfType<OkResult>();
        client.Deleted.ShouldBe((ProjectId, 7));
    }

    public static TheoryData<Exception, string> PaymentTypeRefusals => new()
    {
        { new MustBeAdminException(), "только администратор сайта" },
        { new JoinRpgInvalidUserException(), "уже есть метод оплаты «Наличными»" },
        { new DataException(), "уже есть" },
        { new JoinRpgEntityNotFoundException(7, "PaymentType"), "Метод оплаты не найден" },
    };

    [Theory]
    [MemberData(nameof(PaymentTypeRefusals))]
    public async Task ToggleRefusalIsBadRequestWithReason(Exception refusal, string reason)
    {
        var controller = new FinanceSettingsController(new FakeClient { Failure = refusal });

        var result = await controller.TogglePaymentType(ProjectId, 7);

        result.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBeOfType<string>().ShouldContain(reason);
    }

    [Fact]
    public async Task ToggleGoesToAuthorizedProject()
    {
        var client = new FakeClient();
        var controller = new FinanceSettingsController(client);

        _ = (await controller.TogglePaymentType(ProjectId, 7)).ShouldBeOfType<OkResult>();
        client.Toggled.ShouldBe(new PaymentTypeIdentification(ProjectId, 7));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task CustomPaymentTypeWithoutNameIsRefusedBeforeService(string? name)
    {
        var client = new FakeClient();
        var controller = new FinanceSettingsController(client);

        var result = await controller.CreatePaymentType(ProjectId, new NewPaymentTypeViewModel(PaymentTypeKind.Custom, new UserIdentification(2), name));

        result.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBe("Укажите название метода оплаты");
        client.Created.ShouldBeNull();
    }

    [Fact]
    public async Task OnlinePaymentTypeNeedsNoName()
    {
        var client = new FakeClient();
        var controller = new FinanceSettingsController(client);

        _ = (await controller.CreatePaymentType(ProjectId, new NewPaymentTypeViewModel(PaymentTypeKind.Online, null, null))).ShouldBeOfType<OkResult>();
        client.Created.ShouldNotBeNull().Kind.ShouldBe(PaymentTypeKind.Online);
    }

    private sealed class FakeClient : IFinanceSettingsClient
    {
        public Exception? Failure { get; init; }
        public (ProjectIdentification, int)? Deleted { get; private set; }

        public Task CreateFeeSetting(ProjectIdentification projectId, CreateFeeSettingViewModel model)
            => Failure is null ? Task.CompletedTask : throw Failure;

        public Task DeleteFeeSetting(ProjectIdentification projectId, int projectFeeSettingId)
        {
            if (Failure is not null)
            {
                throw Failure;
            }
            Deleted = (projectId, projectFeeSettingId);
            return Task.CompletedTask;
        }

        public Task<FinanceGeneralSettingsViewModel> GetGeneralSettings(ProjectIdentification projectId) => throw new NotSupportedException();
        public Task SaveGeneralSettings(FinanceGeneralSettingsViewModel model) => throw new NotSupportedException();
        public Task<FinanceFeeSettingsViewModel> GetFeeSettings(ProjectIdentification projectId) => throw new NotSupportedException();
        public Task<FinancePaymentTypesViewModel> GetPaymentTypes(ProjectIdentification projectId) => throw new NotSupportedException();
        public Task<FinanceGoogleDocsExportViewModel> GetGoogleDocsExport(ProjectIdentification projectId) => throw new NotSupportedException();

        public NewPaymentTypeViewModel? Created { get; private set; }

        public Task CreatePaymentType(ProjectIdentification projectId, NewPaymentTypeViewModel model)
        {
            if (Failure is not null)
            {
                throw Failure;
            }
            Created = model;
            return Task.CompletedTask;
        }

        public Task TogglePaymentType(PaymentTypeIdentification paymentTypeId)
        {
            if (Failure is not null)
            {
                throw Failure;
            }
            Toggled = paymentTypeId;
            return Task.CompletedTask;
        }

        public PaymentTypeIdentification? Toggled { get; private set; }
    }
}
