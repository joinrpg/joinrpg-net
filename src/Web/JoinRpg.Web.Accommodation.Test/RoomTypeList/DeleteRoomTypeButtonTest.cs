using Bunit;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Web.Accommodation.RoomTypeList;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Accommodation.Test.RoomTypeList;

/// <summary>
/// Кнопка-остров «Удалить тип» на странице «Поселение»: спрашивает подтверждение, удаляет через API,
/// а отказ сервера показывает и даёт повторить.
/// </summary>
public class DeleteRoomTypeButtonTest : BunitContext
{
    private static readonly AccommodationTypeIdentification TypeId = new(new ProjectIdentification(123), 7);

    private readonly FakeClient client = new();
    private readonly BunitJSModuleInterop dialogModule;

    public DeleteRoomTypeButtonTest()
    {
        Services.AddSingleton<IRoomTypeEditClient>(client);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        dialogModule = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        _ = dialogModule.SetupVoid("showModal", _ => true);
        _ = dialogModule.SetupVoid("closeModal", _ => true);
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    private IRenderedComponent<DeleteRoomTypeButton> RenderButton()
        => Render<DeleteRoomTypeButton>(parameters => parameters
            .Add(c => c.RoomTypeId, TypeId)
            .Add(c => c.Name, "Палатка"));

    private static async Task ClickDeleteAndConfirm(IRenderedComponent<DeleteRoomTypeButton> cut)
    {
        var click = cut.Find("button[aria-label='Удалить']").ClickAsync(new MouseEventArgs());
        var confirm = cut.FindAll("dialog").Single(d => d.TextContent.Contains("Удалить тип размещения «Палатка»?"));
        await confirm.QuerySelectorAll("button").Single(b => b.TextContent.Trim() == "Удалить").ClickAsync(new MouseEventArgs());
        await cut.FindAll("dialog").Single(d => d.TextContent.Contains("Удалить тип размещения «Палатка»?"))
            .TriggerEventAsync("onclose", EventArgs.Empty);
        await click;
    }

    [Fact]
    public async Task ConfirmedDeleteCallsApi()
    {
        var cut = RenderButton();

        await ClickDeleteAndConfirm(cut);

        client.Deleted.ShouldBe([TypeId]);
    }

    [Fact]
    public async Task RefusalIsShownAndDeleteCanBeRetried()
    {
        client.Failure = new ServerProblemException("В комнатах этого типа живут игроки — сначала выселите их");
        var cut = RenderButton();

        await ClickDeleteAndConfirm(cut);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("В комнатах этого типа живут игроки — сначала выселите их"));

        // Кнопка вышла из состояния «в процессе»: повторное нажатие снова спрашивает подтверждение.
        client.Failure = null;
        await ClickDeleteAndConfirm(cut);
        client.Deleted.Count.ShouldBe(2);
    }

    [Fact]
    public async Task UnexpectedFailureShowsGenericText()
    {
        client.Failure = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");
        var cut = RenderButton();

        await ClickDeleteAndConfirm(cut);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Произошла ошибка при удалении типа проживания"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    private sealed class FakeClient : IRoomTypeEditClient
    {
        public List<AccommodationTypeIdentification> Deleted { get; } = [];
        public Exception? Failure { get; set; }

        public Task DeleteRoomType(AccommodationTypeIdentification roomTypeId)
        {
            Deleted.Add(roomTypeId);
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }

        public Task<RoomTypeEditViewModel> GetRoomType(AccommodationTypeIdentification roomTypeId) => throw new NotSupportedException();
        public Task<RoomTypeEditViewModel> GetNewRoomType(ProjectIdentification projectId) => throw new NotSupportedException();
        public Task CreateRoomType(ProjectIdentification projectId, RoomTypeEditViewModel model) => throw new NotSupportedException();
        public Task UpdateRoomType(AccommodationTypeIdentification roomTypeId, RoomTypeEditViewModel model) => throw new NotSupportedException();
        public Task RenameRoomCategory(RoomCategoryIdentification roomCategoryId, RoomCategoryRenameViewModel model) => throw new NotSupportedException();
    }
}
