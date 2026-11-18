using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// Регрессия: выбор персонажа в <see cref="SingleCharacterSelector"/> менял модель через вычисляемое
/// свойство, но не уведомлял EditContext (тип поля модели int?, а селектор отдаёт CharacterIdentification?),
/// поэтому после неудачного сабмита сообщение «Выберите персонажа» не гасло, пока не тронешь другое поле.
/// </summary>
public class InvitePlayerCharacterValidationTest : BunitContext
{
    private const string RequiredMessage = "Выберите персонажа";
    private static readonly ProjectIdentification ProjectId = new(123);

    public InvitePlayerCharacterValidationTest()
    {
        Services.AddSingleton<ICharactersClient>(new StubCharactersClient());
        Services.AddSingleton<IInvitePlayerClient>(new StubInviteClient());
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        _ = Services.AddJoinRpgWebComponents();
        _ = AddBunitPersistentComponentState();
        // Ссылки на игроков здесь не важны — проверяем только валидацию выбора персонажа.
        _ = ComponentFactories.AddStub<JoinUserLinkEditor>();
        // Интерактивный режим: иначе TypedSelector не принимает события ввода.
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task SelectingCharacter_AfterFailedSubmit_ClearsRequiredMessage()
    {
        var cut = Render<InvitePlayer>(parameters => parameters.Add(c => c.ProjectId, ProjectId));

        await cut.Find("form").SubmitAsync();
        cut.Markup.ShouldContain(RequiredMessage);

        // Список персонажей грузится асинхронно — дожидаемся появления пунктов селектора.
        cut.WaitForAssertion(() => cut.FindAll(".join-select__list input").Count.ShouldBe(1));
        cut.FindAll(".join-select__list input")[0].Change(true);

        cut.Markup.ShouldNotContain(RequiredMessage);
    }

    private sealed class StubCharactersClient : ICharactersClient
    {
        public Task<List<CharacterDto>> GetCharacters(ProjectIdentification projectId, CharacterListType listType = CharacterListType.All)
            => Task.FromResult<List<CharacterDto>>(
            [
                new CharacterDto(new CharacterIdentification(projectId, 10), "Первый", "", IsPublic: true),
            ]);
    }

    private sealed class StubInviteClient : IInvitePlayerClient
    {
        public Task<ClaimIdentification> InvitePlayer(CharacterIdentification characterId, string userLink, string claimText)
            => Task.FromResult(new ClaimIdentification(characterId.ProjectId, 1));
    }
}
