using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Web.Accommodation;
using JoinRpg.WebPortal.Managers.Accommodation;

namespace JoinRpg.WebPortal.Managers.Test.Accommodation;

/// <summary>
/// Форма типа проживания: что она показывает из метаданных проекта и что отдаёт сервису.
/// </summary>
public class RoomTypeEditViewServiceTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private readonly FakeAccommodationTypeService typeService = new();

    private RoomTypeEditViewService CreateService() => new(new FakeProjectMetadataRepository(Mock), typeService);

    [Fact]
    public async Task GetRoomType_ShowsMarkdownSourceAndSettings()
    {
        var type = Mock.CreateAccommodationType("Домик", capacity: 6, cost: 1200, isPlayerSelectable: false);
        type.Description = new MarkdownDbValue("**Тепло**");
        Mock.ReInitProjectInfo();

        var model = await CreateService().GetRoomType(new AccommodationTypeIdentification(Mock.ProjectInfo.ProjectId, type.Id));

        model.Name.ShouldBe("Домик");
        model.Capacity.ShouldBe(6);
        model.Cost.ShouldBe(1200);
        model.IsPlayerSelectable.ShouldBeFalse();
        // В форму попадает исходный markdown, а не отрендеренный HTML: иначе сохранение испортит описание.
        model.Description.ShouldBe("**Тепло**");
    }

    [Fact]
    public async Task CreateRoomType_PassesFormToService()
    {
        var projectId = Mock.ProjectInfo.ProjectId;

        await CreateService().CreateRoomType(projectId, new RoomTypeEditViewModel
        {
            Name = "Шатёр",
            Cost = 500,
            Capacity = 3,
            IsPlayerSelectable = false,
            Description = "_Душно_",
        });

        var (createdIn, request) = typeService.Created.ShouldHaveSingleItem();
        createdIn.ShouldBe(projectId);
        request.ShouldBe(new AccommodationTypeRequest("Шатёр", new MarkdownString("_Душно_"), 500, 3, false));
    }

    [Fact]
    public async Task UpdateRoomType_PassesFormToService()
    {
        var typeId = new AccommodationTypeIdentification(Mock.ProjectInfo.ProjectId, 7);

        await CreateService().UpdateRoomType(typeId, new RoomTypeEditViewModel { Name = "Домик", Capacity = 4 });

        var (updatedId, request) = typeService.Updated.ShouldHaveSingleItem();
        updatedId.ShouldBe(typeId);
        request.ShouldBe(new AccommodationTypeRequest("Домик", new MarkdownString(""), 0, 4, true));
    }

    private sealed class FakeAccommodationTypeService : IAccommodationTypeService
    {
        public List<(ProjectIdentification ProjectId, AccommodationTypeRequest Request)> Created { get; } = [];
        public List<(AccommodationTypeIdentification TypeId, AccommodationTypeRequest Request)> Updated { get; } = [];

        public Task<AccommodationTypeIdentification> CreateAccommodationType(ProjectIdentification projectId, AccommodationTypeRequest request)
        {
            Created.Add((projectId, request));
            return Task.FromResult(new AccommodationTypeIdentification(projectId, 1));
        }

        public Task UpdateAccommodationType(AccommodationTypeIdentification accommodationTypeId, AccommodationTypeRequest request)
        {
            Updated.Add((accommodationTypeId, request));
            return Task.CompletedTask;
        }

        public Task DeleteAccommodationType(AccommodationTypeIdentification accommodationTypeId)
            => throw new NotSupportedException();
    }
}
