using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces.ProjectMetadata;

namespace JoinRpg.Services.Impl.Projects.Metadata;

/// <summary>
/// Типы проживания проекта — настройка мастера, часть <see cref="ProjectInfo"/> (ADR015), поэтому
/// меняются через <see cref="IProjectPropsService"/> (ADR009): права, активность проекта,
/// логирование и согласованность <c>Project</c>/<c>ProjectInfo</c> достаются оттуда.
/// </summary>
internal class AccommodationTypeService(
    IProjectPropsService projectPropsService,
    IAccommodationRepository accommodationRepository) : IAccommodationTypeService
{
    /// <inheritdoc />
    public async Task<AccommodationTypeIdentification> CreateAccommodationType(
        ProjectIdentification projectId,
        AccommodationTypeRequest request)
    {
        var entity = await projectPropsService.ChangeProjectProperties(
            projectId,
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            request,
            ctx =>
            {
                var entity = new ProjectAccommodationType
                {
                    ProjectId = ctx.Project.ProjectId,
                    Project = ctx.Project,
                    ProjectAccommodations = [],
                    Desirous = [],
                };
                Apply(
                    entity,
                    ctx.Request.Name,
                    ctx.Request.Description,
                    ctx.Request.Cost,
                    ctx.Request.Capacity,
                    ctx.Request.IsPlayerSelectable);
                ctx.Project.ProjectAccommodationTypes.Add(entity);
                return entity;
            });

        // Id генерируется базой при SaveChanges — читаем уже после возврата из props-сервиса.
        return entity.GetId();
    }

    /// <inheritdoc />
    public Task UpdateAccommodationType(
        AccommodationTypeIdentification accommodationTypeId,
        AccommodationTypeRequest request)
        => projectPropsService.ChangeProjectProperties(
            accommodationTypeId.ProjectId,
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            (AccommodationTypeId: accommodationTypeId, Request: request),
            ctx =>
            {
                var entity = ctx.GetAccommodationTypeForChange(ctx.Request.AccommodationTypeId);
                Apply(
                    entity,
                    ctx.Request.Request.Name,
                    ctx.Request.Request.Description,
                    ctx.Request.Request.Cost,
                    ctx.Request.Request.Capacity,
                    ctx.Request.Request.IsPlayerSelectable);
            });

    /// <inheritdoc />
    public async Task DeleteAccommodationType(AccommodationTypeIdentification accommodationTypeId)
    {
        // Комнаты и их жильцы — оперативные данные, в граф метаданных проекта они не входят
        // (ADR015), поэтому занятость проверяем отдельным запросом, а не по ctx.Project: иначе
        // получилась бы ленивая догрузка, которую операциям props-сервиса делать нельзя (#4987).
        // В аргументы операции найденную комнату не кладём — это EF-сущность, а аргументы уходят
        // в структурный лог.
        var occupiedRoom = await accommodationRepository.GetOccupiedRoomOfType(accommodationTypeId);

        await projectPropsService.ChangeProjectProperties(
            accommodationTypeId.ProjectId,
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            accommodationTypeId,
            ctx =>
            {
                var entity = ctx.GetAccommodationTypeForChange(ctx.Request);

                if (occupiedRoom is not null)
                {
                    throw new RoomIsOccupiedException(occupiedRoom);
                }

                ctx.RemovePermanently(entity);
            });
    }

    private static void Apply(
        ProjectAccommodationType entity,
        string name,
        MarkdownString description,
        int cost,
        int capacity,
        bool isPlayerSelectable)
    {
        entity.Name = ServiceValidation.Required(name);
        entity.Description = new MarkdownDbValue(description.Value);
        entity.Cost = cost;
        entity.Capacity = capacity;
        entity.IsPlayerSelectable = isPlayerSelectable;
    }
}
