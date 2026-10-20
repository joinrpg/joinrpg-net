using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Portal.Controllers.WebApi;
using JoinRpg.Web.Accommodation;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Ручки формы типа проживания: тип склеивается с проектом из query, по которому проверены права,
/// а невалидная форма до сервиса не доходит.
/// </summary>
public class RoomTypeEditControllerTest
{
    private static readonly ProjectIdentification ProjectId = new(1611);

    [Fact]
    public async Task UpdateGluesRoomTypeWithAuthorizedProject()
    {
        var client = new FakeClient();
        var controller = new RoomTypeEditController(client);

        var result = await controller.Update(ProjectId, 7, new RoomTypeEditViewModel { Name = "Шатёр" });

        _ = result.ShouldBeOfType<OkResult>();
        client.UpdatedId.ShouldBe(new AccommodationTypeIdentification(ProjectId, 7));
    }

    [Fact]
    public async Task RenameRoomCategoryGluesCategoryWithAuthorizedProject()
    {
        var client = new FakeClient();
        var controller = new RoomTypeEditController(client);

        var result = await controller.RenameRoomCategory(ProjectId, 12, new RoomCategoryRenameViewModel { Name = "Корпус А" });

        _ = result.ShouldBeOfType<OkResult>();
        client.RenamedId.ShouldBe(new RoomCategoryIdentification(ProjectId, 12));
    }

    [Fact]
    public async Task DeleteGluesRoomTypeWithAuthorizedProject()
    {
        var client = new FakeClient();
        var controller = new RoomTypeEditController(client);

        var result = await controller.Delete(ProjectId, 7);

        _ = result.ShouldBeOfType<OkResult>();
        client.DeletedId.ShouldBe(new AccommodationTypeIdentification(ProjectId, 7));
    }

    [Fact]
    public async Task DeleteOccupiedTypeIsBadRequestWithReason()
    {
        var typeId = new AccommodationTypeIdentification(ProjectId, 7);
        var client = new FakeClient { DeleteFailure = new AccommodationTypeIsOccupiedException(typeId) };
        var controller = new RoomTypeEditController(client);

        var result = await controller.Delete(ProjectId, 7);

        result.ShouldBeOfType<BadRequestObjectResult>().Value
            .ShouldBe("В комнатах этого типа живут игроки — сначала выселите их");
    }

    [Fact]
    public async Task DeleteMissingTypeIsBadRequestWithReason()
    {
        var client = new FakeClient { DeleteFailure = new JoinRpgEntityNotFoundException(7, "ProjectAccommodationType") };
        var controller = new RoomTypeEditController(client);

        var result = await controller.Delete(ProjectId, 7);

        result.ShouldBeOfType<BadRequestObjectResult>().Value
            .ShouldBe("Тип проживания не найден — возможно, его уже удалили");
    }

    [Fact]
    public async Task InvalidModelIsRejectedWithMessages()
    {
        var client = new FakeClient();
        var controller = new RoomTypeEditController(client);
        controller.ModelState.AddModelError(nameof(RoomTypeEditViewModel.Name), "Укажите название типа поселения");

        var result = await controller.Create(ProjectId, new RoomTypeEditViewModel());

        result.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBe("Укажите название типа поселения");
        client.CreatedIn.ShouldBeNull();
    }

    private sealed class FakeClient : IRoomTypeEditClient
    {
        public ProjectIdentification? CreatedIn { get; private set; }
        public AccommodationTypeIdentification? UpdatedId { get; private set; }

        public Task<RoomTypeEditViewModel> GetRoomType(AccommodationTypeIdentification roomTypeId)
            => Task.FromResult(new RoomTypeEditViewModel());

        public Task<RoomTypeEditViewModel> GetNewRoomType(ProjectIdentification projectId)
            => Task.FromResult(new RoomTypeEditViewModel());

        public Task CreateRoomType(ProjectIdentification projectId, RoomTypeEditViewModel model)
        {
            CreatedIn = projectId;
            return Task.CompletedTask;
        }

        public Task UpdateRoomType(AccommodationTypeIdentification roomTypeId, RoomTypeEditViewModel model)
        {
            UpdatedId = roomTypeId;
            return Task.CompletedTask;
        }

        public RoomCategoryIdentification? RenamedId { get; private set; }

        public Task RenameRoomCategory(RoomCategoryIdentification roomCategoryId, RoomCategoryRenameViewModel model)
        {
            RenamedId = roomCategoryId;
            return Task.CompletedTask;
        }

        public AccommodationTypeIdentification? DeletedId { get; private set; }

        public Exception? DeleteFailure { get; init; }

        public Task DeleteRoomType(AccommodationTypeIdentification roomTypeId)
        {
            DeletedId = roomTypeId;
            return DeleteFailure is null ? Task.CompletedTask : Task.FromException(DeleteFailure);
        }
    }
}
