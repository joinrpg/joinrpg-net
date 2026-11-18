using Bunit;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Web.Accommodation.Rooms;
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
/// Кнопка-остров «Выселить всех» на странице «Поселение»: причину отказа сервера (например, проект заблокирован)
/// показывает, а не заменяет общим текстом (#5735).
/// </summary>
public class UnOccupyAllButtonTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(123);

    private readonly FakeClient client = new();

    public UnOccupyAllButtonTest()
    {
        Services.AddSingleton<IAccommodationRoomsClient>(client);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        var dialogModule = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        _ = dialogModule.SetupVoid("showModal", _ => true);
        _ = dialogModule.SetupVoid("closeModal", _ => true);
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    private async Task<IRenderedComponent<UnOccupyAllButton>> ClickAndConfirm()
    {
        var cut = Render<UnOccupyAllButton>(parameters => parameters.Add(c => c.ProjectId, ProjectId));
        var click = cut.Find("button").ClickAsync(new MouseEventArgs());
        var confirm = cut.FindAll("dialog").Single(d => d.TextContent.Contains("выселить ВСЕХ участников"));
        await confirm.QuerySelectorAll("button").Single(b => b.TextContent.Trim() == "Выселить всех").ClickAsync(new MouseEventArgs());
        await cut.FindAll("dialog").Single(d => d.TextContent.Contains("выселить ВСЕХ участников"))
            .TriggerEventAsync("onclose", EventArgs.Empty);
        await click;
        return cut;
    }

    [Fact]
    public async Task RefusalReasonIsShown()
    {
        client.Failure = new ServerProblemException("Проект заблокирован на время восстановления данных, изменения в нём сейчас невозможны");

        var cut = await ClickAndConfirm();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain(
            "Произошла ошибка при выселении: Проект заблокирован на время восстановления данных, изменения в нём сейчас невозможны"));
    }

    [Fact]
    public async Task UnexpectedFailureShowsGenericText()
    {
        client.Failure = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");

        var cut = await ClickAndConfirm();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Произошла ошибка при выселении"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    private sealed class FakeClient : IAccommodationRoomsClient
    {
        public Exception? Failure { get; set; }

        public Task UnOccupyAll(ProjectIdentification projectId)
            => Failure is null ? Task.CompletedTask : Task.FromException(Failure);

        public Task<RoomTypeRoomsViewModel> GetRooms(AccommodationTypeIdentification typeId) => throw new NotSupportedException();
        public Task<RoomTypeRoomsViewModel> AddRooms(AccommodationTypeIdentification typeId, string roomNames) => throw new NotSupportedException();
        public Task RenameRoom(AccommodationRoomIdentification roomId, string name) => throw new NotSupportedException();
        public Task DeleteRoom(AccommodationRoomIdentification roomId) => throw new NotSupportedException();
        public Task OccupyRoom(AccommodationRoomIdentification roomId, IReadOnlyCollection<AccommodationRequestIdentification> groupIds) => throw new NotSupportedException();
        public Task UnOccupyGroup(AccommodationRequestIdentification groupId) => throw new NotSupportedException();
        public Task UnOccupyRoom(AccommodationRoomIdentification roomId) => throw new NotSupportedException();
        public Task UnOccupyRoomType(AccommodationTypeIdentification typeId) => throw new NotSupportedException();
    }
}
