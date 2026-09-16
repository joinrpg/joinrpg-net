using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

/// <summary>
/// Поселение — часть агрегата проекта (ADR009): типы поселения и комнаты это его настройки, а
/// заселение — данные о комнатах проекта. Поэтому все мутации идут через
/// <see cref="IProjectPropsService"/>, хотя в снимок метаданных проекта поселение не входит и
/// грузится именованными загрузчиками хэндла (<see cref="IProjectAccommodationWriteAccess"/>).
/// </summary>
internal class AccommodationServiceImpl(
    IProjectPropsService projectPropsService,
    IAccommodationRepository accommodationRepository,
    IUserRepository userRepository,
    ICurrentUserAccessor currentUserAccessor,
    IEmailService emailService) : IAccommodationService
{
    /// <remarks>
    /// Прежняя проверка «ProjectId не может быть 0» убрана как избыточная: проект теперь
    /// резолвится через <see cref="IProjectPropsService"/>, и несуществующий проект (в том числе
    /// нулевой) отваливается на загрузке хэндла.
    /// </remarks>
    public async Task<ProjectAccommodationType?> SaveRoomTypeAsync(ProjectAccommodationType roomType)
    {
        return await projectPropsService.ChangeProjectPropertiesAsync(
            new ProjectIdentification(roomType.ProjectId),
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            roomType,
            async ctx =>
            {
                if (ctx.Request.Id == 0)
                {
                    ctx.AddEntity(ctx.Request);
                    return ctx.Request;
                }

                // null (а не исключение) — если тип не найден или принадлежит другому проекту.
                // Ровно то же, что делала прежняя ручная проверка ProjectId.
                var result = await ctx.Accommodation.LoadRoomType(ctx.Request.Id);
                if (result is null)
                {
                    return null;
                }

                result.Name = ctx.Request.Name;
                result.Cost = ctx.Request.Cost;
                result.Capacity = ctx.Request.Capacity;
                result.Description = ctx.Request.Description;
                result.IsAutoFilledAccommodation = ctx.Request.IsAutoFilledAccommodation;
                result.IsInfinite = ctx.Request.IsInfinite;
                result.IsPlayerSelectable = ctx.Request.IsPlayerSelectable;
                return result;
            });
    }

    public async Task<IReadOnlyCollection<ProjectAccommodationType>> GetRoomTypesAsync(int projectId)
        => await accommodationRepository.GetAccommodationForProject(projectId).ConfigureAwait(false);

    public async Task<ProjectAccommodationType?> GetRoomTypeAsync(int roomTypeId)
        => await accommodationRepository.GetRoomTypeWithDesirous(roomTypeId);

    public async Task RemoveRoomType(int projectId, int roomTypeId)
        => await projectPropsService.ChangeProjectPropertiesAsync(
            new ProjectIdentification(projectId),
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            roomTypeId,
            async ctx =>
            {
                var entity = await LoadRoomType(ctx, ctx.Request);

                var occupiedRoom = entity.ProjectAccommodations.FirstOrDefault(pa => pa.IsOccupied());
                if (occupiedRoom != null)
                {
                    throw new RoomIsOccupiedException(occupiedRoom);
                }

                ctx.RemovePermanently(entity);
            });

    public async Task<IEnumerable<ProjectAccommodation>> AddRooms(int projectId, int roomTypeId, string rooms)
        => await projectPropsService.ChangeProjectPropertiesAsync(
            new ProjectIdentification(projectId),
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            new { roomTypeId, rooms },
            async ctx =>
            {
                //TODO: Implement rooms names checking
                var roomType = await LoadRoomType(ctx, ctx.Request.roomTypeId);

                var result = ParseRooms(ctx.Request.rooms, roomType, projectId).ToList();
                foreach (var room in result)
                {
                    ctx.AddEntity(room);
                }
                return (IEnumerable<ProjectAccommodation>)result;
            });

    public async Task EditRoom(int projectId, int roomTypeId, int roomId, string name)
        => await projectPropsService.ChangeProjectPropertiesAsync(
            new ProjectIdentification(projectId),
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            new { roomTypeId, roomId, name },
            async ctx =>
            {
                var entity = await LoadRoom(ctx, ctx.Request.roomId, ctx.Request.roomTypeId);
                entity.Name = ctx.Request.name;
            });

    public async Task DeleteRoom(int projectId, int roomTypeId, int roomId)
        => await projectPropsService.ChangeProjectPropertiesAsync(
            new ProjectIdentification(projectId),
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            new { roomTypeId, roomId },
            async ctx =>
            {
                var entity = await LoadRoom(ctx, ctx.Request.roomId, ctx.Request.roomTypeId);
                if (entity.IsOccupied())
                {
                    throw new RoomIsOccupiedException(entity);
                }
                ctx.RemovePermanently(entity);
            });

    public async Task OccupyRoom(OccupyRequest request)
    {
        var (room, changed) = await projectPropsService.ChangeProjectPropertiesAsync(
            new ProjectIdentification(request.ProjectId),
            Permission.CanSetPlayersAccommodations,
            ProjectActiveRequirement.MustBeActive,
            request,
            async ctx =>
            {
                var room = await LoadRoom(ctx, ctx.Request.RoomId, roomTypeId: null);
                var accommodationRequests =
                    await ctx.Accommodation.LoadAccommodationRequests(ctx.Request.AccommodationRequestIds);

                foreach (var accommodationRequest in accommodationRequests)
                {
                    var freeSpace = room.GetRoomFreeSpace();

                    if (freeSpace < accommodationRequest.Subjects.Count)
                    {
                        throw new JoinRpgInsufficientRoomSpaceException(room);
                    }

                    accommodationRequest.AccommodationId = room.Id;
                    accommodationRequest.Accommodation = room;
                }

                return (room, accommodationRequests.SelectMany(ar => ar.Subjects).ToArray());
            });

        await emailService.Email(await CreateRoomEmail<OccupyRoomEmail>(room, changed));
    }

    public async Task UnOccupyRoom(UnOccupyRequest request)
    {
        var unoccupied = await projectPropsService.ChangeProjectPropertiesAsync(
            new ProjectIdentification(request.ProjectId),
            Permission.CanSetPlayersAccommodations,
            ProjectActiveRequirement.MustBeActive,
            request,
            async ctx =>
            {
                var accommodationRequests =
                    await ctx.Accommodation.LoadAccommodationRequests([ctx.Request.AccommodationRequestId]);
                var accommodationRequest = accommodationRequests.SingleOrDefault()
                    ?? throw new JoinRpgEntityNotFoundException(
                        ctx.Request.AccommodationRequestId, nameof(AccommodationRequest));

                var room = await LoadRoom(
                    ctx,
                    accommodationRequest.AccommodationId
                        ?? throw new JoinRpgEntityNotFoundException(
                            ctx.Request.AccommodationRequestId, nameof(ProjectAccommodation)),
                    roomTypeId: null);

                return UnOccupy([(room, [accommodationRequest])]);
            });

        await SendUnOccupyEmails(unoccupied);
    }

    public async Task UnOccupyRoomAll(UnOccupyAllRequest request)
    {
        var unoccupied = await projectPropsService.ChangeProjectPropertiesAsync(
            new ProjectIdentification(request.ProjectId),
            Permission.CanSetPlayersAccommodations,
            ProjectActiveRequirement.MustBeActive,
            request,
            async ctx =>
            {
                var room = await LoadRoom(ctx, ctx.Request.RoomId, roomTypeId: null);
                return UnOccupy([(room, room.Inhabitants.ToArray())]);
            });

        await SendUnOccupyEmails(unoccupied);
    }

    public async Task UnOccupyRoomType(int projectId, int roomTypeId)
        => await UnOccupyRooms(projectId, roomTypeId);

    public async Task UnOccupyAll(int projectId) => await UnOccupyRooms(projectId, roomTypeId: null);

    /// <summary>
    /// Выселяет все заселённые комнаты проекта (при необходимости — только одного типа поселения)
    /// одной операцией: одна проверка прав, одно сохранение, письма — по комнате, после сохранения.
    /// </summary>
    private async Task UnOccupyRooms(int projectId, int? roomTypeId)
    {
        var unoccupied = await projectPropsService.ChangeProjectPropertiesAsync(
            new ProjectIdentification(projectId),
            Permission.CanSetPlayersAccommodations,
            ProjectActiveRequirement.MustBeActive,
            new { roomTypeId },
            async ctx =>
            {
                var rooms = await ctx.Accommodation.LoadOccupiedRooms(ctx.Request.roomTypeId);
                return UnOccupy([.. rooms.Select(room => (room, room.Inhabitants.ToArray()))]);
            },
            operationName: roomTypeId is null ? nameof(UnOccupyAll) : nameof(UnOccupyRoomType));

        await SendUnOccupyEmails(unoccupied);
    }

    /// <summary>
    /// Снимает заселение и возвращает то, из чего потом (после сохранения) строятся письма:
    /// комнату и заявки игроков, которых из неё выселили.
    /// </summary>
    private static List<(ProjectAccommodation Room, Claim[] Changed)> UnOccupy(
        IReadOnlyCollection<(ProjectAccommodation Room, AccommodationRequest[] Requests)> rooms)
    {
        var result = new List<(ProjectAccommodation Room, Claim[] Changed)>();

        foreach (var (room, requests) in rooms)
        {
            foreach (var request in requests)
            {
                request.AccommodationId = null;
                request.Accommodation = null;
            }

            result.Add((room, requests.SelectMany(r => r.Subjects).ToArray()));
        }

        return result;
    }

    private async Task SendUnOccupyEmails(IReadOnlyCollection<(ProjectAccommodation Room, Claim[] Changed)> unoccupied)
    {
        foreach (var (room, changed) in unoccupied)
        {
            await emailService.Email(await CreateRoomEmail<UnOccupyRoomEmail>(room, changed));
        }
    }

    private static async Task<ProjectAccommodationType> LoadRoomType(ProjectMutationContext ctx, int roomTypeId)
        => await ctx.Accommodation.LoadRoomType(roomTypeId)
            ?? throw new JoinRpgEntityNotFoundException(roomTypeId, nameof(ProjectAccommodationType));

    private static async Task<ProjectAccommodation> LoadRoom(ProjectMutationContext ctx, int roomId, int? roomTypeId)
    {
        var result = await ctx.Accommodation.LoadRoom(roomId)
            ?? throw new JoinRpgEntityNotFoundException(roomId, nameof(ProjectAccommodation));

        if (roomTypeId.HasValue && result.AccommodationTypeId != roomTypeId.Value)
        {
            throw new ArgumentException($@"Room {roomId} is from different room type than specified", nameof(roomTypeId));
        }

        return result;
    }

    /// <summary>
    /// Разбирает строку с номерами комнат: «1,2,5-8» — перечисление и диапазоны.
    /// </summary>
    private static IEnumerable<ProjectAccommodation> ParseRooms(
        string rooms,
        ProjectAccommodationType roomType,
        int projectId)
    {
        ProjectAccommodation CreateRoom(string name)
            => new()
            {
                Name = name,
                AccommodationTypeId = roomType.Id,
                ProjectId = projectId,
                ProjectAccommodationType = roomType,
            };

        foreach (var roomCandidate in rooms.Split(','))
        {
            var rangePos = roomCandidate.IndexOf('-');
            if (rangePos > -1)
            {
                if (int.TryParse(roomCandidate.Substring(0, rangePos).Trim(), out var roomsRangeStart)
                    && int.TryParse(roomCandidate.Substring(rangePos + 1).Trim(), out var roomsRangeEnd)
                    && roomsRangeStart < roomsRangeEnd)
                {
                    while (roomsRangeStart <= roomsRangeEnd)
                    {
                        yield return CreateRoom(roomsRangeStart.ToString());
                        roomsRangeStart++;
                    }
                    // Range was defined correctly, we can continue to next item
                    continue;
                }
            }

            yield return CreateRoom(roomCandidate.Trim());
        }
    }

    private async Task<T> CreateRoomEmail<T>(ProjectAccommodation room, Claim[] changed)
        where T : RoomEmailBase, new()
        => new()
        {
            Changed = changed,
            Initiator = await userRepository.GetById(currentUserAccessor.UserId),
            ProjectName = room.Project.ProjectName,
            Recipients = room.GetSubscriptions().ToList(),
            Room = room,
            Text = new MarkdownDbValue(),
        };
}
