using AngleSharp.Dom;
using Bunit;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Web.Accommodation.Rooms;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Accommodation.Test.Rooms;

/// <summary>
/// Контрол расселения на странице «Комнаты»: что предлагает, что зовёт и как откатывается.
/// </summary>
public class RoomTypeRoomsControlTest : BunitContext
{
    private readonly FakeAccommodationRoomsClient client = new();
    private readonly BunitJSModuleInterop dialogModule;

    public RoomTypeRoomsControlTest()
    {
        Services.AddSingleton<IAccommodationRoomsClient>(client);
        Services.AddSingleton<IUriLocator<ClaimIdentification>>(new FakeClaimUriLocator());
        Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeUserUriLocator());
        Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        dialogModule = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        _ = dialogModule.SetupVoid("showModal", _ => true);
        _ = dialogModule.SetupVoid("closeModal", _ => true);
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    private IRenderedComponent<RoomTypeRoomsControl> RenderControl(RoomTypeRoomsViewModel model)
        => Render<RoomTypeRoomsControl>(parameters => parameters.Add(c => c.Model, model));

    private static IElement RoomRow(IRenderedComponent<RoomTypeRoomsControl> cut, int roomId)
        => cut.Find($"tr[data-room-id='{roomId}']");

    private static string Occupancy(IRenderedComponent<RoomTypeRoomsControl> cut, int roomId)
        => RoomRow(cut, roomId).QuerySelector(".rooms-room-occupancy")!.TextContent.Trim();

    /// <summary>Нажать кнопку диалога, внутри которого есть <paramref name="contentSelector"/>, и закрыть его так, как это сделал бы браузер</summary>
    private static async Task PressDialogButton(IRenderedComponent<RoomTypeRoomsControl> cut, string contentSelector, string buttonText)
    {
        var dialog = cut.FindAll("dialog").Single(d => d.QuerySelector(contentSelector) is not null);
        var button = dialog.QuerySelectorAll("button").Single(b => b.TextContent.Contains(buttonText));
        await button.ClickAsync(new MouseEventArgs());
        await cut.FindAll("dialog").Single(d => d.QuerySelector(contentSelector) is not null)
            .TriggerEventAsync("onclose", EventArgs.Empty);
    }

    [Fact]
    public async Task OccupyDialog_OffersOnlyFittingGroups()
    {
        var resident = RoomsModel.Group(1, persons: 1);
        var pair = RoomsModel.Group(2, persons: 2);
        var single = RoomsModel.Group(3, persons: 1);
        var anotherSingle = RoomsModel.Group(4, persons: 1);
        var cut = RenderControl(RoomsModel.Create(
            rooms: [RoomsModel.Room(1, resident)],
            unassigned: [pair, single, anotherSingle]));

        RoomRow(cut, 1).QuerySelector("[data-action='occupy']")!.Click();

        // Пара в комнату с одним свободным местом не влезает и не предлагается вовсе.
        var offered = cut.FindAll(".rooms-people-list .list-group-item");
        offered.Select(i => i.GetAttribute("data-group-id")).ShouldBe(["3", "4"]);
        cut.Find(".rooms-free-space").TextContent.ShouldBe("1");

        // Выбрали одного — второй уже не влезет вместе с ним.
        await cut.Find(".rooms-people-list [data-group-id='3']").ClickAsync(new MouseEventArgs());
        cut.Find(".rooms-people-list [data-group-id='3']").ClassList.ShouldContain("active");
        cut.Find(".rooms-people-list [data-group-id='4']").ClassList.ShouldContain("disabled");
        cut.Find(".rooms-free-space").TextContent.ShouldBe("0");
        cut.Find(".rooms-used-space").TextContent.ShouldBe("2");

        await PressDialogButton(cut, ".rooms-people-list", "Заселить");

        cut.WaitForAssertion(() => client.Calls.ShouldBe(["OccupyRoom 1: 3"]));
        Occupancy(cut, 1).ShouldBe("2 / 2");
        cut.FindAll(".rooms-unassigned .list-group-item").Count.ShouldBe(2);
    }

    [Fact]
    public async Task Occupy_ServerRefusal_RollsBackAndShowsReason()
    {
        client.Failure = new AccommodationRoomsOperationRefusedException("В комнате не хватает мест");
        var cut = RenderControl(RoomsModel.Create(
            rooms: [RoomsModel.Room(1)],
            unassigned: [RoomsModel.Group(3, persons: 1)]));

        RoomRow(cut, 1).QuerySelector("[data-action='occupy']")!.Click();
        await cut.Find(".rooms-people-list [data-group-id='3']").ClickAsync(new MouseEventArgs());
        await PressDialogButton(cut, ".rooms-people-list", "Заселить");

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Произошла ошибка при заселении в комнату: В комнате не хватает мест"));
        Occupancy(cut, 1).ShouldBe("0 / 2");
        cut.FindAll(".rooms-unassigned .list-group-item").Count.ShouldBe(1);
    }

    [Fact]
    public async Task Kick_MovesGroupToUnassigned()
    {
        var cut = RenderControl(RoomsModel.Create(rooms: [RoomsModel.Room(1, RoomsModel.Group(5, persons: 1))]));

        await RoomRow(cut, 1).QuerySelector("[data-action='kick']")!.ClickAsync(new MouseEventArgs());

        client.Calls.ShouldBe(["UnOccupyGroup 5"]);
        Occupancy(cut, 1).ShouldBe("0 / 2");
        cut.FindAll(".rooms-unassigned .list-group-item").Count.ShouldBe(1);
    }

    [Fact]
    public async Task Kick_Failure_RollsBack()
    {
        client.Failure = new HttpRequestException("сеть");
        var cut = RenderControl(RoomsModel.Create(rooms: [RoomsModel.Room(1, RoomsModel.Group(5, persons: 1))]));

        await RoomRow(cut, 1).QuerySelector("[data-action='kick']")!.ClickAsync(new MouseEventArgs());

        Occupancy(cut, 1).ShouldBe("1 / 2");
        cut.FindAll(".rooms-unassigned").ShouldBeEmpty();
        // Причина сетевой ошибки мастеру не нужна — только что не получилось.
        cut.Markup.ShouldContain("Произошла ошибка при выселении из комнаты");
        cut.Markup.ShouldNotContain("сеть");
    }

    [Fact]
    public async Task EvictRoom_AsksAndEvictsEverybody()
    {
        var cut = RenderControl(RoomsModel.Create(
            rooms: [RoomsModel.Room(1, RoomsModel.Group(5, persons: 1), RoomsModel.Group(6, persons: 1))]));

        RoomRow(cut, 1).QuerySelector("[data-action='evict']")!.Click();
        cut.Find(".rooms-confirm").TextContent.ShouldBe("Выселить всех из комнаты '1'?");
        await PressDialogButton(cut, ".rooms-confirm", "Да");

        cut.WaitForAssertion(() => client.Calls.ShouldBe(["UnOccupyRoom 1"]));
        Occupancy(cut, 1).ShouldBe("0 / 2");
    }

    [Fact]
    public async Task PlaceAll_OccupiesRoomByRoom()
    {
        var cut = RenderControl(RoomsModel.Create(
            rooms: [RoomsModel.Room(1), RoomsModel.Room(2)],
            unassigned: [RoomsModel.Group(3, persons: 2), RoomsModel.Group(4, persons: 1)]));

        await cut.Find("[data-action='place-all']").ClickAsync(new MouseEventArgs());

        client.Calls.ShouldBe(["OccupyRoom 1: 3", "OccupyRoom 2: 4"]);
        cut.FindAll(".rooms-unassigned").ShouldBeEmpty();
    }

    [Fact]
    public async Task Delete_RemovesRoomAndRestoresOnFailure()
    {
        client.Failure = new AccommodationRoomsOperationRefusedException("В комнате живут игроки — сначала выселите их");
        var cut = RenderControl(RoomsModel.Create(rooms: [RoomsModel.Room(1)]));

        RoomRow(cut, 1).QuerySelector("[data-action='delete']")!.Click();
        cut.Find(".rooms-confirm").TextContent.ShouldBe("Удалить комнату 1?");
        await PressDialogButton(cut, ".rooms-confirm", "Да");

        cut.WaitForAssertion(() => client.Calls.ShouldBe(["DeleteRoom 1"]));
        cut.FindAll("tr[data-room-id='1']").Count.ShouldBe(1);
    }

    [Fact]
    public void RoomButtons_AreDisabledByState()
    {
        var cut = RenderControl(RoomsModel.Create(
            rooms: [RoomsModel.Room(1, RoomsModel.Group(5, persons: 2)), RoomsModel.Room(2)]));

        // Полная комната: заселять некуда, удалять нельзя.
        RoomRow(cut, 1).QuerySelector("[data-action='occupy']")!.HasAttribute("disabled").ShouldBeTrue();
        RoomRow(cut, 1).QuerySelector("[data-action='delete']")!.HasAttribute("disabled").ShouldBeTrue();
        RoomRow(cut, 1).QuerySelector("[data-action='evict']")!.HasAttribute("disabled").ShouldBeFalse();
        // Пустая: выселять некого.
        RoomRow(cut, 2).QuerySelector("[data-action='evict']")!.HasAttribute("disabled").ShouldBeTrue();
        RoomRow(cut, 2).QuerySelector("[data-action='delete']")!.HasAttribute("disabled").ShouldBeFalse();
        // Нерасселённых нет — «Заселить всех» выключено, а «Выселить всех» есть кого.
        cut.Find("[data-action='place-all']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-action='kick-all']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void WithoutManageRight_RoomManagementIsHidden()
    {
        var cut = RenderControl(RoomsModel.Create(
            rooms: [RoomsModel.Room(1, RoomsModel.Group(5, persons: 1))],
            canManageRooms: false));

        cut.FindAll("[data-action='add'], [data-action='rename'], [data-action='delete']").ShouldBeEmpty();
        cut.FindAll("[data-action='occupy'], [data-action='evict'], [data-action='place-all'], [data-action='kick-all']").Count.ShouldBe(4);
    }

    [Fact]
    public void WithoutAssignRight_AccommodationIsHidden()
    {
        var cut = RenderControl(RoomsModel.Create(
            rooms: [RoomsModel.Room(1, RoomsModel.Group(5, persons: 1))],
            canAssignRooms: false));

        cut.FindAll("[data-action='occupy'], [data-action='evict'], [data-action='place-all'], [data-action='kick-all']").ShouldBeEmpty();
        RoomRow(cut, 1).QuerySelectorAll("[data-action='kick']").ShouldBeEmpty();
        cut.FindAll("[data-action='add'], [data-action='rename'], [data-action='delete']").Count.ShouldBe(3);
    }

    [Fact]
    public void ResidentsAreSeparatedBySpace()
    {
        var cut = RenderControl(RoomsModel.Create(rooms: [RoomsModel.Room(1, RoomsModel.Group(5, persons: 2))]));

        var text = RoomRow(cut, 1).QuerySelector(".acc-request")!.TextContent;
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ShouldBe("Персонаж 5-0 (Игрок 5-0) Персонаж 5-1 (Игрок 5-1)");
    }

    private static IEnumerable<IElement> MutationButtons(IRenderedComponent<RoomTypeRoomsControl> cut)
        => cut.FindAll("[data-action='add'], [data-action='place-all'], [data-action='kick-all'], [data-action='occupy'], [data-action='evict'], [data-action='rename'], [data-action='delete'], [data-action='kick']");

    /// <summary>Ввести имя в диалог имени комнаты и нажать «ОК»</summary>
    private static async Task EnterRoomName(IRenderedComponent<RoomTypeRoomsControl> cut, string name, string submit)
    {
        cut.Find("#rooms-room-name").Input(name);
        await PressDialogButton(cut, "#rooms-room-name", submit);
    }

    [Fact]
    public async Task WhileRequestIsInFlight_OtherMutationsAreBlocked()
    {
        // Откат делается снимком всего состояния: если бы вторую операцию можно было начать, пока
        // первая в полёте, откат первой вернул бы и результат второй.
        var first = RoomsModel.Group(5, persons: 1);
        var second = RoomsModel.Group(6, persons: 1);
        client.Pending = new TaskCompletionSource();
        var cut = RenderControl(RoomsModel.Create(rooms: [RoomsModel.Room(1, first), RoomsModel.Room(2, second)]));

        RoomRow(cut, 1).QuerySelector("[data-action='kick']")!.Click();

        cut.WaitForAssertion(() => MutationButtons(cut).ShouldAllBe(b => b.HasAttribute("disabled")));
        await RoomRow(cut, 2).QuerySelector("[data-action='kick']")!.ClickAsync(new MouseEventArgs());
        client.Calls.ShouldBe(["UnOccupyGroup 5"]);

        client.Pending.SetException(new HttpRequestException("сеть"));

        cut.WaitForAssertion(() => Occupancy(cut, 1).ShouldBe("1 / 2"));
        Occupancy(cut, 2).ShouldBe("1 / 2");
        RoomRow(cut, 2).QuerySelector("[data-action='kick']")!.HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public async Task Rename_ShowsNewNameAndBlocksWhileInFlight()
    {
        client.Pending = new TaskCompletionSource();
        var cut = RenderControl(RoomsModel.Create(rooms: [RoomsModel.Room(1)]));

        RoomRow(cut, 1).QuerySelector("[data-action='rename']")!.Click();
        await EnterRoomName(cut, "Люкс", "Сохранить");

        cut.WaitForAssertion(() => client.Calls.ShouldBe(["RenameRoom 1: Люкс"]));
        RoomRow(cut, 1).QuerySelector(".rooms-room-name")!.TextContent.ShouldBe("Люкс");
        RoomRow(cut, 1).QuerySelector("[data-action='rename']")!.HasAttribute("disabled").ShouldBeTrue();

        client.Pending.SetResult();

        cut.WaitForAssertion(() => RoomRow(cut, 1).QuerySelector("[data-action='rename']")!.HasAttribute("disabled").ShouldBeFalse());
        RoomRow(cut, 1).QuerySelector(".rooms-room-name")!.TextContent.ShouldBe("Люкс");
    }

    [Fact]
    public async Task Rename_Failure_RestoresName()
    {
        client.Failure = new AccommodationRoomsOperationRefusedException("Укажите название комнаты");
        var cut = RenderControl(RoomsModel.Create(rooms: [RoomsModel.Room(1)]));

        RoomRow(cut, 1).QuerySelector("[data-action='rename']")!.Click();
        await EnterRoomName(cut, "Люкс", "Сохранить");

        cut.WaitForAssertion(() => cut.Find(".rooms-message").TextContent
            .ShouldBe("Произошла ошибка при добавлении или изменении комнат: Укажите название комнаты"));
        RoomRow(cut, 1).QuerySelector(".rooms-room-name")!.TextContent.ShouldBe("1");
    }

    [Fact]
    public async Task AddRooms_AppliesModelFromResponse()
    {
        client.NextRooms = RoomsModel.Create(rooms: [RoomsModel.Room(1), RoomsModel.Room(7), RoomsModel.Room(8)]);
        var cut = RenderControl(RoomsModel.Create(rooms: [RoomsModel.Room(1)]));

        cut.Find("[data-action='add']").Click();
        await EnterRoomName(cut, "7-8", "Добавить");

        cut.WaitForAssertion(() => client.Calls.ShouldBe(["AddRooms 1: 7-8"]));
        cut.FindAll("tr[data-room-id]").Select(r => r.GetAttribute("data-room-id")).ShouldBe(["1", "7", "8"]);
    }

    [Fact]
    public async Task NoRooms_AddDialogOpensByItself()
    {
        client.NextRooms = RoomsModel.Create(rooms: [RoomsModel.Room(1)]);
        var cut = RenderControl(RoomsModel.Create());

        // Диалог открылся сам: в нём можно сразу ввести номера.
        cut.WaitForAssertion(() => dialogModule.Invocations["showModal"].Any().ShouldBeTrue());
        await EnterRoomName(cut, "1", "Добавить");

        cut.WaitForAssertion(() => client.Calls.ShouldBe(["AddRooms 1: 1"]));
        cut.FindAll("tr[data-room-id]").Count.ShouldBe(1);
    }

    [Fact]
    public void NoRooms_WithoutManageRight_AddDialogDoesNotOpen()
    {
        var cut = RenderControl(RoomsModel.Create(canManageRooms: false));

        cut.Render();

        dialogModule.Invocations["showModal"].Any().ShouldBeFalse();
    }

    [Fact]
    public async Task KickAll_EvictsOwnTypeAndRereadsModel()
    {
        client.NextRooms = RoomsModel.Create(rooms: [RoomsModel.Room(1, RoomsModel.Group(6, persons: 1, typeId: 2))],
            unassigned: [RoomsModel.Group(5, persons: 1)]);
        var cut = RenderControl(RoomsModel.Create(
            rooms: [RoomsModel.Room(1, RoomsModel.Group(5, persons: 1), RoomsModel.Group(6, persons: 1, typeId: 2))]));

        cut.Find("[data-action='kick-all']").Click();
        await PressDialogButton(cut, ".rooms-confirm", "Да");

        cut.WaitForAssertion(() => client.Calls.ShouldBe(["UnOccupyRoomType 1", "GetRooms 1"]));
        Occupancy(cut, 1).ShouldBe("1 / 2");
        cut.FindAll(".rooms-unassigned .list-group-item").Count.ShouldBe(1);
    }

    [Fact]
    public async Task KickAll_RereadFailure_KeepsShownResult()
    {
        client.NextRooms = null;
        var cut = RenderControl(RoomsModel.Create(
            rooms: [RoomsModel.Room(1, RoomsModel.Group(5, persons: 1), RoomsModel.Group(6, persons: 1, typeId: 2))]));

        cut.Find("[data-action='kick-all']").Click();
        await PressDialogButton(cut, ".rooms-confirm", "Да");

        cut.WaitForAssertion(() => client.Calls.ShouldBe(["UnOccupyRoomType 1", "GetRooms 1"]));
        // Жилец другого типа той же категории остался, свой — выселен.
        RoomRow(cut, 1).QuerySelector(".acc-request")!.TextContent.ShouldContain("Игрок 6-0");
        cut.Find(".rooms-unassigned").TextContent.ShouldContain("Игрок 5-0");
        cut.Find(".rooms-message").TextContent.ShouldBeEmpty();
    }

    [Fact]
    public void SharedPool_ShowsSiblingsAndTypeLabels()
    {
        var cut = RenderControl(RoomsModel.Create(
            rooms: [RoomsModel.Room(1, RoomsModel.Group(6, persons: 1, typeCapacity: 1, typeId: 2, typeName: "Люкс на одного"))],
            unassigned: [RoomsModel.Group(5, persons: 1, typeName: "Двушка")],
            siblingTypeNames: ["Люкс на одного"]));

        cut.Find(".rooms-shared-pool").TextContent.ShouldContain("Комнаты общие с типами: Люкс на одного");
        RoomRow(cut, 1).QuerySelector(".acc-type")!.TextContent.ShouldBe("Люкс на одного");
        cut.Find(".rooms-unassigned .acc-type").TextContent.ShouldBe("Двушка");
        // Жилец «на одного» занял двухместную комнату целиком (ADR018).
        Occupancy(cut, 1).ShouldBe("1 / 1");
    }

    private sealed class FakeUserUriLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new($"https://example.com/user/{target.UserId?.Value}");
    }

    private sealed class FakeClaimUriLocator : IUriLocator<ClaimIdentification>
    {
        public Uri GetUri(ClaimIdentification target) => new($"https://example.com/claim/{target.ClaimId}");
    }

    private sealed class FakeAccommodationRoomsClient : IAccommodationRoomsClient
    {
        public List<string> Calls { get; } = [];

        public Exception? Failure { get; set; }

        /// <summary>Ответ сервера, который тест отдаст сам, когда захочет. Задан — мутации висят до него</summary>
        public TaskCompletionSource? Pending { get; set; }

        /// <summary>Что вернёт перечитывание комнат; <c>null</c> — перечитывание падает</summary>
        public RoomTypeRoomsViewModel? NextRooms { get; set; }

        private Task Record(string call)
        {
            Calls.Add(call);
            if (Pending is { } pending)
            {
                return pending.Task;
            }
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }

        public Task<RoomTypeRoomsViewModel> GetRooms(AccommodationTypeIdentification typeId)
        {
            Calls.Add($"GetRooms {typeId.AccommodationTypeId}");
            return NextRooms is null
                ? Task.FromException<RoomTypeRoomsViewModel>(new HttpRequestException("сеть"))
                : Task.FromResult(NextRooms);
        }

        public Task<RoomTypeRoomsViewModel> AddRooms(AccommodationTypeIdentification typeId, string roomNames)
        {
            Calls.Add($"AddRooms {typeId.AccommodationTypeId}: {roomNames}");
            return Task.FromResult(NextRooms ?? throw new InvalidOperationException("Тест не задал ответ"));
        }

        public Task RenameRoom(AccommodationRoomIdentification roomId, string name) => Record($"RenameRoom {roomId.RoomId}: {name}");

        public Task DeleteRoom(AccommodationRoomIdentification roomId) => Record($"DeleteRoom {roomId.RoomId}");

        public Task OccupyRoom(AccommodationRoomIdentification roomId, IReadOnlyCollection<AccommodationRequestIdentification> groupIds)
            => Record($"OccupyRoom {roomId.RoomId}: {string.Join(", ", groupIds.Select(g => g.AccommodationRequestId))}");

        public Task UnOccupyGroup(AccommodationRequestIdentification groupId) => Record($"UnOccupyGroup {groupId.AccommodationRequestId}");

        public Task UnOccupyRoom(AccommodationRoomIdentification roomId) => Record($"UnOccupyRoom {roomId.RoomId}");

        public Task UnOccupyRoomType(AccommodationTypeIdentification typeId) => Record($"UnOccupyRoomType {typeId.AccommodationTypeId}");
    }
}
