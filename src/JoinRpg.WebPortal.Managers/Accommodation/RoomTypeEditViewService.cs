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
        return RoomTypeEditViewModelBuilder.Build(
            projectInfo.AccommodationSettings.GetTypeById(roomTypeId),
            projectInfo.AccommodationSettings);
    }

    public async Task<RoomTypeEditViewModel> GetNewRoomType(ProjectIdentification projectId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        return new RoomTypeEditViewModel
        {
            RoomCategories = RoomTypeEditViewModelBuilder.BuildCategories(projectInfo.AccommodationSettings),
        };
    }

    public async Task CreateRoomType(ProjectIdentification projectId, RoomTypeEditViewModel model)
        => _ = await accommodationTypeService.CreateAccommodationType(
            projectId,
            RoomTypeEditViewModelBuilder.ToRequest(model),
            RoomTypeEditViewModelBuilder.ToRoomCategorySelection(projectId, model));

    public Task UpdateRoomType(AccommodationTypeIdentification roomTypeId, RoomTypeEditViewModel model)
        => accommodationTypeService.UpdateAccommodationType(
            roomTypeId,
            RoomTypeEditViewModelBuilder.ToRequest(model));

    public Task RenameRoomCategory(RoomCategoryIdentification roomCategoryId, RoomCategoryRenameViewModel model)
        => accommodationTypeService.RenameRoomCategory(roomCategoryId, model.Name.Trim());
}

/// <summary>
/// Перевод между формой типа проживания и доменом.
/// </summary>
internal static class RoomTypeEditViewModelBuilder
{
    public static RoomTypeEditViewModel Build(AccommodationTypeInfo typeInfo, ProjectAccommodationSettings settings)
    {
        var category = settings.GetRoomCategoryById(typeInfo.RoomCategoryId);
        return new()
        {
            Name = typeInfo.Name,
            Cost = typeInfo.Cost,
            Capacity = typeInfo.Capacity,
            IsPlayerSelectable = typeInfo.IsPlayerSelectable,
            Description = typeInfo.Description.Value,
            // Соседей отбираем по Id, а не по имени: имена типов не уникальны, а поле имени в форме
            // меняется, пока мастер печатает.
            RoomCategory = new RoomCategoryOptionViewModel(
                category.Id.RoomCategoryId,
                category.Name,
                [.. settings.GetTypesOfCategory(category.Id).Where(type => type.Id != typeInfo.Id).Select(type => type.Name)]),
        };
    }

    public static IReadOnlyList<RoomCategoryOptionViewModel> BuildCategories(ProjectAccommodationSettings settings)
        => [.. settings.RoomCategories
            .OrderBy(category => category.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(category => new RoomCategoryOptionViewModel(
                category.Id.RoomCategoryId,
                category.Name,
                [.. settings.GetTypesOfCategory(category.Id).Select(type => type.Name)]))];

    public static AccommodationTypeRequest ToRequest(RoomTypeEditViewModel model)
        => new(
            model.Name,
            new MarkdownString(model.Description ?? ""),
            model.Cost,
            model.Capacity,
            model.IsPlayerSelectable);

    /// <summary>
    /// Пустое название новой категории — категория получит имя типа.
    /// </summary>
    public static RoomCategorySelection ToRoomCategorySelection(ProjectIdentification projectId, RoomTypeEditViewModel model)
        => model.RoomCategoryChoice == RoomCategoryChoice.Existing && model.ExistingRoomCategoryId is int categoryId
            ? new ExistingRoomCategory(new RoomCategoryIdentification(projectId, categoryId))
            : new NewRoomCategory(string.IsNullOrWhiteSpace(model.NewRoomCategoryName) ? model.Name : model.NewRoomCategoryName.Trim());
}
