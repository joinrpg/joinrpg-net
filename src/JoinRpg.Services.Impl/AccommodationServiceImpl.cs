using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Impl.Accommodation;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

/// <summary>
/// Сервис комнат и заселения. Управление комнатами переведено на агрегат плана поселения
/// (ADR018, PR 4) и идёт через <see cref="IAccommodationPropsService"/>; заселение пока живёт
/// на легаси-базе <see cref="DbServiceImplBase"/> и переедет в PR 5.
/// </summary>
internal class AccommodationServiceImpl : DbServiceImplBase, IAccommodationService
{
    private IEmailService EmailService { get; }

    private readonly IAccommodationPropsService accommodationPropsService;

    public async Task OccupyRoom(OccupyRequest request)
    {
        var room = await UnitOfWork.GetDbSet<ProjectAccommodation>()
            .Include(r => r.Inhabitants)
            .Include(r => r.ProjectAccommodationType)
            .Where(r => r.ProjectId == request.ProjectId && r.Id == request.RoomId)
            .FirstOrDefaultAsync();

        var accommodationRequests = await UnitOfWork.GetDbSet<AccommodationRequest>()
            .Include(r => r.Subjects.Select(s => s.Player))
            .Include(r => r.Project)
            .Where(r => r.ProjectId == request.ProjectId && request.AccommodationRequestIds.Contains(r.Id))
            .ToListAsync();

        _ = room.Project.RequestMasterAccess(CurrentUserId, Permission.CanSetPlayersAccommodations);

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


        await UnitOfWork.SaveChangesAsync();

        await EmailService.Email(await CreateRoomEmail<OccupyRoomEmail>(room, accommodationRequests.SelectMany(ar => ar.Subjects).ToArray()));
    }

    private async Task<T> CreateRoomEmail<T>(ProjectAccommodation room, Claim[] changed)
    where T : RoomEmailBase, new()
    {
        return new T()
        {
            Changed = changed,
            Initiator = await GetCurrentUser(),
            ProjectName = room.Project.ProjectName,
            Recipients = room.GetSubscriptions().ToList(),
            Room = room,
            Text = new MarkdownDbValue(),
        };
    }

    public async Task UnOccupyRoom(UnOccupyRequest request)
    {

        var accommodationRequest = await UnitOfWork.GetDbSet<AccommodationRequest>()
            .Include(r => r.Subjects.Select(s => s.Player))
            .Include(r => r.Project)
            .Where(r => r.ProjectId == request.ProjectId && r.Id == request.AccommodationRequestId)
            .FirstOrDefaultAsync();

        var room = await UnitOfWork.GetDbSet<ProjectAccommodation>()
            .Include(r => r.Inhabitants)
            .Include(r => r.ProjectAccommodationType)
            .Where(r => r.ProjectId == request.ProjectId && r.Id == accommodationRequest.AccommodationId)
            .FirstOrDefaultAsync();

        await UnOccupyRoomImpl(room, new[] { accommodationRequest });
    }

    private async Task UnOccupyRoomImpl(ProjectAccommodation room,
        IReadOnlyCollection<AccommodationRequest> accommodationRequests)
    {
        _ = room.Project.RequestMasterAccess(CurrentUserId, Permission.CanSetPlayersAccommodations);

        foreach (var request in accommodationRequests)
        {
            request.AccommodationId = null;
            request.Accommodation = null;
        }

        await UnitOfWork.SaveChangesAsync();

        await EmailService.Email(
            await CreateRoomEmail<UnOccupyRoomEmail>(room, accommodationRequests.SelectMany(x => x.Subjects).ToArray()));
    }

    public async Task UnOccupyRoomAll(UnOccupyAllRequest request)
    {
        var room = await GetRoomQuery(request.ProjectId)
            .Where(r => r.Id == request.RoomId)
            .FirstOrDefaultAsync();

        await UnOccupyRoomImpl(room, room.Inhabitants.ToList());
    }

    private IQueryable<ProjectAccommodation> GetRoomQuery(int projectId)
    {
        return UnitOfWork.GetDbSet<ProjectAccommodation>()
            .Include(r => r.Project)
            .Include(r => r.Inhabitants)
            .Include(r => r.ProjectAccommodationType)
            .Include(r => r.Inhabitants.Select(i => i.Subjects.Select(c => c.Player)))
            .Where(r => r.ProjectId == projectId);
    }

    public async Task UnOccupyRoomType(int projectId, int roomTypeId)
    {
        var rooms = await GetRoomQuery(projectId)
            .Where(r => r.Inhabitants.Any())
            .Where(r => r.AccommodationTypeId == roomTypeId)
            .ToListAsync();

        foreach (var room in rooms)
        {
            await UnOccupyRoomImpl(room, room.Inhabitants.ToList());
        }
    }

    public async Task UnOccupyAll(int projectId)
    {
        var rooms = await GetRoomQuery(projectId)
            .Where(r => r.Inhabitants.Any())
            .ToListAsync();

        foreach (var room in rooms)
        {
            await UnOccupyRoomImpl(room, room.Inhabitants.ToList());
        }
    }

    public async Task<IReadOnlyCollection<AccommodationRoomIdentification>> AddRooms(
        RoomCategoryIdentification categoryId,
        IReadOnlyCollection<string> roomNames)
    {
        //TODO: Implement rooms names checking
        var created = await accommodationPropsService.ChangePlan(
            categoryId,
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            roomNames,
            ctx =>
            {
                var created = new List<ProjectAccommodation>();
                foreach (var name in ctx.Request)
                {
                    var room = new ProjectAccommodation
                    {
                        Name = name,
                        AccommodationTypeId = ctx.Category.Id,
                        ProjectId = ctx.Category.ProjectId,
                        ProjectAccommodationType = ctx.Category,
                        Inhabitants = [],
                    };
                    ctx.AddEntity(room);
                    created.Add(room);
                }
                return created;
            });

        // Id генерируются базой при SaveChanges — читаем уже после возврата из props-сервиса.
        return [.. created.Select(room => room.GetId())];
    }

    public Task RenameRoom(AccommodationRoomIdentification roomId, string name)
        => accommodationPropsService.ChangePlanForRoom(
            roomId,
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            (RoomId: roomId, Name: name),
            ctx => ctx.GetRoomForChange(ctx.Request.RoomId).Name = ServiceValidation.Required(ctx.Request.Name));

    public Task DeleteRoom(AccommodationRoomIdentification roomId)
        => accommodationPropsService.ChangePlanForRoom(
            roomId,
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            roomId,
            ctx =>
            {
                // Заселённость смотрим по доменному снимку плана, а не по навигации EF-сущности.
                if (ctx.Plan.GetRoom(ctx.Request).IsOccupied)
                {
                    throw new RoomIsOccupiedException(ctx.Request);
                }

                ctx.RemoveEntity(ctx.GetRoomForChange(ctx.Request));
            });

    public AccommodationServiceImpl(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ICurrentUserAccessor currentUserAccessor,
        IAccommodationPropsService accommodationPropsService) : base(unitOfWork, currentUserAccessor)
    {
        EmailService = emailService;
        this.accommodationPropsService = accommodationPropsService;
    }
}
