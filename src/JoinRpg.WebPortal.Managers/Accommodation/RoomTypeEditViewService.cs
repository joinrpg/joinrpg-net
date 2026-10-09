using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Web.Accommodation;

namespace JoinRpg.WebPortal.Managers.Accommodation;

/// <summary>
/// Серверная сторона формы создания и изменения типа проживания. Права на изменение проверяет
/// <see cref="IAccommodationTypeService"/>, сам тип берётся из метаданных проекта (ADR015).
/// </summary>
internal class RoomTypeEditViewService(
    IProjectMetadataRepository projectMetadataRepository,
    IAccommodationTypeService accommodationTypeService) : IRoomTypeEditClient
{
    public async Task<RoomTypeEditViewModel> GetRoomType(AccommodationTypeIdentification roomTypeId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(roomTypeId.ProjectId);
        return RoomTypeEditViewModelBuilder.Build(projectInfo.AccommodationSettings.GetTypeById(roomTypeId));
    }

    public async Task CreateRoomType(ProjectIdentification projectId, RoomTypeEditViewModel model)
        => _ = await accommodationTypeService.CreateAccommodationType(
            projectId,
            RoomTypeEditViewModelBuilder.ToRequest(model));

    public Task UpdateRoomType(AccommodationTypeIdentification roomTypeId, RoomTypeEditViewModel model)
        => accommodationTypeService.UpdateAccommodationType(
            roomTypeId,
            RoomTypeEditViewModelBuilder.ToRequest(model));
}

/// <summary>
/// Перевод между формой типа проживания и доменом.
/// </summary>
internal static class RoomTypeEditViewModelBuilder
{
    public static RoomTypeEditViewModel Build(AccommodationTypeInfo typeInfo) => new()
    {
        Name = typeInfo.Name,
        Cost = typeInfo.Cost,
        Capacity = typeInfo.Capacity,
        IsPlayerSelectable = typeInfo.IsPlayerSelectable,
        Description = typeInfo.Description.Value,
    };

    public static AccommodationTypeRequest ToRequest(RoomTypeEditViewModel model)
        => new(
            model.Name,
            new MarkdownString(model.Description ?? ""),
            model.Cost,
            model.Capacity,
            model.IsPlayerSelectable);
}
