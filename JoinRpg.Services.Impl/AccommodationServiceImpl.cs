using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Email;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Practices.ServiceLocation;

namespace JoinRpg.Services.Impl
{
    [UsedImplicitly]
    public class AccommodationServiceImpl : DbServiceImplBase, IAccommodationService
    {
        private IEmailService EmailService { get; }

        public async Task<ProjectAccommodationType> SaveRoomTypeAsync(ProjectAccommodationType roomType)
        {
            if (roomType.ProjectId == 0)
                throw new ActivationException("Inconsistent state. ProjectId can't be 0");

            ProjectAccommodationType result;

            if (roomType.Id != 0)
            {
                result = await UnitOfWork.GetDbSet<ProjectAccommodationType>().FindAsync(roomType.Id).ConfigureAwait(false);
                if (result?.ProjectId != roomType.ProjectId)
                {
                    return null;
                }
                result.Name = roomType.Name;
                result.Cost = roomType.Cost;
                result.Capacity = roomType.Capacity;
                result.Description = roomType.Description;
                result.IsAutoFilledAccommodation = roomType.IsAutoFilledAccommodation;
                result.IsInfinite = roomType.IsInfinite;
                result.IsPlayerSelectable = roomType.IsPlayerSelectable;
            }
            else
            {
                result = UnitOfWork.GetDbSet<ProjectAccommodationType>().Add(roomType);
            }
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);
            return result;
        }

        public async Task<IReadOnlyCollection<ProjectAccommodationType>> GetRoomTypesAsync(int projectId)
        {
            return await AccomodationRepository.GetAccommodationForProject(projectId).ConfigureAwait(false);
        }

        public async Task<ProjectAccommodationType> GetRoomTypeAsync(int roomTypeId)
        {
            return await UnitOfWork.GetDbSet<ProjectAccommodationType>()
                .Include(x => x.ProjectAccommodations)
                .Include(x => x.Desirous)
                .FirstOrDefaultAsync(x => x.Id == roomTypeId);
        }

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

            room.Project.RequestMasterAccess(CurrentUserId, acl => acl.CanSetPlayersAccommodations);

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
        where T: RoomEmailBase, new()
        {
            return new T()
            {
                Changed = changed,
                Initiator = await GetCurrentUser(),
                ProjectName = room.Project.ProjectName,
                Recipients = room.GetSubscriptions().ToList(),
                Room = room,
                Text = new MarkdownString(),
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

            await UnOccupyRoomImpl(room, new[] {accommodationRequest});
        }

        private async Task UnOccupyRoomImpl(ProjectAccommodation room,
            IReadOnlyCollection<AccommodationRequest> accommodationRequests)
        {
            room.Project.RequestMasterAccess(CurrentUserId, acl => acl.CanSetPlayersAccommodations);

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

        public async Task RemoveRoomType(int accomodationTypeId)
        {
            var entity = UnitOfWork.GetDbSet<ProjectAccommodationType>().Find(accomodationTypeId);

            if (entity == null)
            {
                throw new JoinRpgEntityNotFoundException(accomodationTypeId, "ProjectAccommodationType");
            }

            var occupiedRoom =
                entity.ProjectAccommodations.FirstOrDefault(pa => pa.IsOccupied());
            if (occupiedRoom != null)
            {
                throw new RoomIsOccupiedException(occupiedRoom);
            }
            UnitOfWork.GetDbSet<ProjectAccommodationType>().Remove(entity);
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

        }

        public async Task<IEnumerable<ProjectAccommodation>> AddRooms(int projectId, int roomTypeId, string rooms)
        {
            //TODO: Implement rooms names checking

            ProjectAccommodationType roomType = UnitOfWork.GetDbSet<ProjectAccommodationType>().Find(roomTypeId);
            if (roomType == null)
                throw new JoinRpgEntityNotFoundException(roomTypeId, typeof(ProjectAccommodationType).Name);
            if (roomType.ProjectId != projectId)
                throw new ArgumentException($"Room type {roomTypeId} is from another project than specified", nameof(roomTypeId));

            // Internal function
            // Creates new room using name and parameters from given room info
            ProjectAccommodation CreateRoom(string name)
                => new ProjectAccommodation
                {
                    Name = name,
                    AccommodationTypeId = roomTypeId,
                    ProjectId = projectId,
                    ProjectAccommodationType = roomType,
                };

            // Internal function
            // Iterates through rooms list and creates object for each room from a list
            IEnumerable<ProjectAccommodation> CreateRooms(string r)
            {
                foreach (string roomCandidate in r.Split(','))
                {
                    int rangePos = roomCandidate.IndexOf('-');
                    if (rangePos > -1)
                    {
                        if (int.TryParse(roomCandidate.Substring(0, rangePos).Trim(), out int roomsRangeStart)
                            && int.TryParse(roomCandidate.Substring(rangePos + 1).Trim(), out int roomsRangeEnd)
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

            IEnumerable<ProjectAccommodation> result =
                UnitOfWork.GetDbSet<ProjectAccommodation>().AddRange(CreateRooms(rooms));
            await UnitOfWork.SaveChangesAsync();
            return result;
        }

        private ProjectAccommodation GetRoom(int roomId, int? projectId = null, int? roomTypeId = null)
        {
            var result = UnitOfWork.GetDbSet<ProjectAccommodation>().Find(roomId);

            if (result == null)
                throw new JoinRpgEntityNotFoundException(roomId, typeof(ProjectAccommodation).Name);
            if (projectId.HasValue)
            {
                if (result.ProjectId != projectId.Value)
                    throw new ArgumentException($"Room {roomId} is from different project than specified", nameof(projectId));
            }
            if (roomTypeId.HasValue)
            {
                if (result.AccommodationTypeId != roomTypeId.Value)
                    throw new ArgumentException($"Room {roomId} is from different room type than specified", nameof(projectId));
            }

            return result;
        }

        public async Task EditRoom(int roomId, string name, int? projectId = null, int? roomTypeId = null)
        {
            var entity = GetRoom(roomId, projectId, roomTypeId);
            entity.Name = name;
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task DeleteRoom(int roomId, int? projectId = null, int? roomTypeId = null)
        {
            var entity = GetRoom(roomId, projectId, roomTypeId);
            if (entity.IsOccupied())
            {
                throw new RoomIsOccupiedException(entity);
            }
            UnitOfWork.GetDbSet<ProjectAccommodation>().Remove(entity);
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);
        }

        public AccommodationServiceImpl(IUnitOfWork unitOfWork, IEmailService emailService) : base(unitOfWork)
        {
            EmailService = emailService;
        }
    }
}
