using Bunit;
using Bunit.TestDoubles;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Accommodation.Test;

public class RoomTypeEditFormTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(123);
    private static readonly AccommodationTypeIdentification RoomTypeId = new(ProjectId, 7);

    private readonly FakeRoomTypeEditClient client = new();

    public RoomTypeEditFormTest()
    {
        Services.AddSingleton<IRoomTypeEditClient>(client);
        Services.AddSingleton<IAccommodationUriLocator>(new FakeUriLocator());
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    private IRenderedComponent<RoomTypeEditForm> RenderCreate()
        => Render<RoomTypeEditForm>(parameters => parameters.Add(c => c.ProjectId, ProjectId));

    private IRenderedComponent<RoomTypeEditForm> RenderEdit()
        => Render<RoomTypeEditForm>(parameters => parameters
            .Add(c => c.ProjectId, ProjectId)
            .Add(c => c.RoomTypeId, RoomTypeId));

    [Fact]
    public void CreateForm_IsPrefilledWithDefaults()
    {
        var cut = RenderCreate();

        cut.FindAll("input[type=number]")[1].GetAttribute("value").ShouldBe(RoomTypeEditViewModel.DefaultCapacity.ToString());
        cut.Find("input[type=checkbox]").HasAttribute("checked").ShouldBeTrue();
        cut.Find("button[type=submit]").TextContent.ShouldBe("Добавить");
        client.Loaded.ShouldBeEmpty("Пустой форме нечего загружать");
    }

    /// <summary>
    /// Клик по подписи должен попадать в поле, как в прежней MVC-форме: каждая подпись ссылается
    /// на существующий элемент ввода, а чекбокс — именно тот, который переключает флаг.
    /// </summary>
    [Fact]
    public void EveryLabelPointsToItsInput()
    {
        var cut = RenderCreate();

        // У строки кнопок подпись пустая — ей и ссылаться не на что.
        var labels = cut.FindAll("label.control-label").Where(label => label.TextContent.Trim().Length > 0).ToList();
        labels.Count.ShouldBe(5);
        foreach (var label in labels)
        {
            var target = label.GetAttribute("for");
            target.ShouldNotBeNullOrEmpty($"У подписи «{label.TextContent}» нет for");
            cut.FindAll($"#{target}").ShouldHaveSingleItem($"Подпись «{label.TextContent}» ссылается в никуда")
                .TagName.ShouldBeOneOf("INPUT", "TEXTAREA");
        }

        var checkboxId = cut.Find("input[type=checkbox]").Id;
        labels.ShouldContain(label => label.GetAttribute("for") == checkboxId);
    }

    [Fact]
    public void CreateForm_SubmitsToClientAndGoesToList()
    {
        var cut = RenderCreate();

        cut.Find("input[type=text]").Change("Шатёр");
        cut.FindAll("input[type=number]")[0].Change("1500");
        cut.Find("textarea").Change("**Тепло**");
        cut.Find("form").Submit();

        var created = client.Created.ShouldHaveSingleItem();
        created.ProjectId.ShouldBe(ProjectId);
        created.Model.Name.ShouldBe("Шатёр");
        created.Model.Cost.ShouldBe(1500);
        created.Model.Capacity.ShouldBe(RoomTypeEditViewModel.DefaultCapacity);
        created.Model.Description.ShouldBe("**Тепло**");
        Services.GetRequiredService<BunitNavigationManager>().Uri.ShouldEndWith("/123/rooms");
    }

    [Fact]
    public void EditForm_LoadsTypeAndSavesIt()
    {
        client.Existing = new RoomTypeEditViewModel { Name = "Домик", Cost = 300, Capacity = 4, IsPlayerSelectable = false, Description = "Старое" };

        var cut = RenderEdit();

        cut.Find("input[type=text]").GetAttribute("value").ShouldBe("Домик");
        cut.Find("input[type=checkbox]").HasAttribute("checked").ShouldBeFalse();
        cut.Find("button[type=submit]").TextContent.ShouldBe("Сохранить");

        cut.FindAll("input[type=number]")[1].Change("6");
        cut.Find("form").Submit();

        var updated = client.Updated.ShouldHaveSingleItem();
        updated.RoomTypeId.ShouldBe(RoomTypeId);
        updated.Model.Name.ShouldBe("Домик");
        updated.Model.Capacity.ShouldBe(6);
        client.Created.ShouldBeEmpty();
    }

    [Fact]
    public void InvalidForm_ShowsRussianMessagesAndDoesNotCallClient()
    {
        var cut = RenderCreate();

        cut.FindAll("input[type=number]")[1].Change("0");
        cut.Find("form").Submit();

        cut.Markup.ShouldContain("Укажите название типа поселения");
        cut.Markup.ShouldContain("Укажите количество мест в номере — целое число от 1 до 1000");
        client.Created.ShouldBeEmpty();
    }

    [Fact]
    public void ServerError_IsShownInForm()
    {
        client.Error = new InvalidOperationException("Проект закрыт");
        var cut = RenderCreate();

        cut.Find("input[type=text]").Change("Шатёр");
        cut.Find("form").Submit();

        cut.Markup.ShouldContain("Проект закрыт");
        Services.GetRequiredService<BunitNavigationManager>().Uri.ShouldNotEndWith("/rooms");
    }

    private sealed class FakeUriLocator : IAccommodationUriLocator
    {
        public Uri GetRoomTypesListUri(ProjectIdentification projectId) => new($"/{projectId.Value}/rooms", UriKind.Relative);
    }

    private sealed class FakeRoomTypeEditClient : IRoomTypeEditClient
    {
        public RoomTypeEditViewModel Existing { get; set; } = new();
        public Exception? Error { get; set; }
        public List<AccommodationTypeIdentification> Loaded { get; } = [];
        public List<(ProjectIdentification ProjectId, RoomTypeEditViewModel Model)> Created { get; } = [];
        public List<(AccommodationTypeIdentification RoomTypeId, RoomTypeEditViewModel Model)> Updated { get; } = [];

        public Task<RoomTypeEditViewModel> GetRoomType(AccommodationTypeIdentification roomTypeId)
        {
            Loaded.Add(roomTypeId);
            return Task.FromResult(Existing);
        }

        public Task CreateRoomType(ProjectIdentification projectId, RoomTypeEditViewModel model)
        {
            if (Error is not null)
            {
                return Task.FromException(Error);
            }
            Created.Add((projectId, model));
            return Task.CompletedTask;
        }

        public Task UpdateRoomType(AccommodationTypeIdentification roomTypeId, RoomTypeEditViewModel model)
        {
            if (Error is not null)
            {
                return Task.FromException(Error);
            }
            Updated.Add((roomTypeId, model));
            return Task.CompletedTask;
        }
    }
}
