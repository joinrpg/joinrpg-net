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
    }
}
