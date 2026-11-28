using Bunit;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Web.AdminTools.Advertisement;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.AdminTools.Test.Advertisement;

/// <summary>
/// Остров создания канала рекламы: диалог со списком чатов бота.
/// </summary>
public class PostingChannelCreatePanelTest : BunitContext
{
    private readonly FakePostingChannelClient client = new();

    public PostingChannelCreatePanelTest()
    {
        Services.AddSingleton<IPostingChannelClient>(client);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        // Диалог открывается через JS-модуль WebComponents.
        var joinDialogModule = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        joinDialogModule.SetupVoid("showModal", _ => true);
        joinDialogModule.SetupVoid("closeModal", _ => true);
    }

    [Fact]
    public void OpenDialog_WithChats_ShowsDropdownNotSpinner()
    {
        client.Response = new AvailableChatsResponse(true, "testbot",
            [new AvailableChatDto(new TelegramChatId(-100111), "Канал А")]);
        var cut = Render<PostingChannelCreatePanel>();

        FindButton(cut, "Добавить канал").Click();

        cut.WaitForAssertion(() => client.GetAvailableChatsCalls.ShouldBe(1));
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Канал А");
            cut.Markup.ShouldNotContain("Идёт загрузка");
        });
    }

    [Fact]
    public async Task OpenDialog_SlowLoad_DialogContentUpdatesWhenLoadCompletes()
    {
        // Регрессия прода: диалог открывается сразу со спиннером, а когда загрузка завершается,
        // содержимое обязано обновиться в уже открытом диалоге — без перерисовки пользователь
        // навсегда видел «Идёт загрузка...»
        var pending = new TaskCompletionSource<AvailableChatsResponse>();
        client.GetAvailableChatsHandler = () => pending.Task;
        var cut = Render<PostingChannelCreatePanel>();

        FindButton(cut, "Добавить канал").Click();
        cut.WaitForAssertion(() => client.GetAvailableChatsCalls.ShouldBe(1));

        await cut.InvokeAsync(() => pending.SetResult(new AvailableChatsResponse(true, "testbot",
            [new AvailableChatDto(new TelegramChatId(-100111), "Канал А")])));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Канал А");
            cut.Markup.ShouldNotContain("Идёт загрузка");
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void OpenDialog_DisabledBot_ShowsWarning()
    {
        client.Response = new AvailableChatsResponse(false, null, []);
        var cut = Render<PostingChannelCreatePanel>();

        FindButton(cut, "Добавить канал").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Telegram-бот не настроен"));
    }

    [Fact]
    public void OpenDialog_EmptyChats_ShowsHint()
    {
        client.Response = new AvailableChatsResponse(true, "testbot", []);
        var cut = Render<PostingChannelCreatePanel>();

        FindButton(cut, "Добавить канал").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Нет доступных чатов"));
    }

    private static AngleSharp.Dom.IElement FindButton(IRenderedComponent<PostingChannelCreatePanel> cut, string text)
        => cut.FindAll("button").First(button => button.TextContent.Contains(text));

    private sealed class FakePostingChannelClient : IPostingChannelClient
    {
        public AvailableChatsResponse Response { get; set; } = new(false, null, []);
        public Func<Task<AvailableChatsResponse>>? GetAvailableChatsHandler { get; set; }
        public int GetAvailableChatsCalls { get; private set; }

        public Task<AvailableChatsResponse> GetAvailableChats()
        {
            GetAvailableChatsCalls++;
            return GetAvailableChatsHandler?.Invoke() ?? Task.FromResult(Response);
        }

        public Task CreateChannel(CreateChannelRequest request) => Task.CompletedTask;
    }
}
