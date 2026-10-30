using JoinRpg.Domain;
using JoinRpg.DomainTypes;
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
    }
}
