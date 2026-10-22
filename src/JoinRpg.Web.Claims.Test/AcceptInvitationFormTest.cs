using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// Остров «Принятие приглашения»: причину отказа сервера показывает,
/// а сырой текст неожиданной ошибки — нет (#5735).
/// </summary>
public class AcceptInvitationFormTest : BunitContext
{
    private static readonly ClaimIdentification ClaimId = new(new ProjectIdentification(123), 45);

    private readonly FakeClaimOperationClient client = new();

    public AcceptInvitationFormTest()
    {
        Services.AddSingleton<IClaimOperationClient>(client);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private async Task<IRenderedComponent<AcceptInvitationForm>> Submit()
    {
        var cut = Render<AcceptInvitationForm>(parameters => parameters
            .Add(c => c.ClaimId, ClaimId)
            .Add(c => c.RequestedSensitiveData, new RequestedSensitiveDataView(false, false, false)));
        await cut.Find("form").SubmitAsync();
        return cut;
    }

    // #5706: игрок видит, к каким именно данным просят доступ.
    [Fact]
    public void ConsentNamesRequestedData()
    {
        var cut = Render<AcceptInvitationForm>(parameters => parameters
            .Add(c => c.ClaimId, ClaimId)
            .Add(c => c.RequestedSensitiveData, new RequestedSensitiveDataView(Passport: true, RegistrationAddress: false, BirthDate: true)));

        cut.Markup.ShouldContain("Мастера игры просят доступ к паспортным данным и дате рождения из вашего профиля.");
    }

    [Fact]
    public void NoConsentCheckboxWhenNothingRequested()
    {
        var cut = Render<AcceptInvitationForm>(parameters => parameters
            .Add(c => c.ClaimId, ClaimId)
            .Add(c => c.RequestedSensitiveData, new RequestedSensitiveDataView(false, false, false)));

        cut.Markup.ShouldNotContain("Предоставить доступ к защищённым данным");
    }

    [Fact]
    public async Task RefusalReasonIsShown()
    {
        client.Failure = new ServerProblemException("Проект заблокирован на время восстановления данных, изменения в нём сейчас невозможны");

        var cut = await Submit();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain(
            "Проект заблокирован на время восстановления данных, изменения в нём сейчас невозможны"));
    }

    [Fact]
    public async Task UnexpectedFailureShowsGenericText()
    {
        client.Failure = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");

        var cut = await Submit();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось принять приглашение"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }
}
