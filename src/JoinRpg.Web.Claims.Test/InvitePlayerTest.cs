using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// Остров «Пригласить игрока»: причину отказа сервера показывает у ссылки на игрока,
/// а сырой текст неожиданной ошибки — нет (#5735).
/// </summary>
public class InvitePlayerTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(123);
    private static readonly CharacterIdentification CharacterId = new(ProjectId, 45);

    private readonly FakeClient client = new();

    public InvitePlayerTest()
    {
        Services.AddSingleton<IInvitePlayerClient>(client);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        // Выбор персонажа и игроков ходят на сервер и в JS — для отказа сервера они не важны
        _ = ComponentFactories.AddStub<SingleCharacterSelector>();
        _ = ComponentFactories.AddStub<JoinUserLinkEditor>();
    }

    private async Task<IRenderedComponent<InvitePlayer>> Submit()
    {
        var cut = Render<InvitePlayer>(parameters => parameters
            .Add(c => c.ProjectId, ProjectId)
            .Add(c => c.PreselectedCharacterId, CharacterId)
            .Add(c => c.PreselectedUserLink, "42"));
        await cut.Find("form").SubmitAsync();
        return cut;
    }

    [Fact]
    public async Task RefusalReasonIsShown()
    {
        client.Failure = new ServerProblemException("Персонаж уже занят");

        var cut = await Submit();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось пригласить игроков: 42 — Персонаж уже занят"));
    }

    [Fact]
    public async Task UnexpectedFailureShowsGenericText()
    {
        client.Failure = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");

        var cut = await Submit();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось пригласить игроков: 42 — произошла ошибка"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    private sealed class FakeClient : IInvitePlayerClient
    {
        public Exception? Failure { get; set; }

        public Task<ClaimIdentification> InvitePlayer(CharacterIdentification CharacterId, string UserLink, string ClaimText)
            => Failure is null
                ? Task.FromResult(new ClaimIdentification(CharacterId.ProjectId, 1))
                : Task.FromException<ClaimIdentification>(Failure);
    }
}
