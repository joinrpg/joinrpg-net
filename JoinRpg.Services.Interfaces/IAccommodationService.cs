using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
    public interface IAccommodationService
    {
        /// <summary>
        /// Adds new room type
        /// </summary>
        Task<ProjectAccommodationType> SaveRoomTypeAsync(ProjectAccommodationType roomType);

        Task RemoveRoomType(int roomTypeId);

        /// <summary>
        /// Adds rooms to specified room type of specified project
        /// </summary>
        Task<IEnumerable<ProjectAccommodation>> AddRooms(int projectId, int roomTypeId, string rooms);

        /// <summary>
        /// Changes room name
        /// </summary>
        Task EditRoom(int roomId, string name, int? projectId = null, int? roomTypeId = null);

        /// <summary>
        /// Deletes specified room
        /// </summary>
        Task DeleteRoom(int roomId, int? projectId = null, int? roomTypeId = null);

        /// <summary>
        /// Returns all room types for specified project Id
        /// </summary>
        Task<IReadOnlyCollection<ProjectAccommodationType>> GetRoomTypes(int projectId);

        Task<ProjectAccommodationType> GetAccommodationByIdAsync(int accId);

        /// <summary>
        /// Move inhabitants to room
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task OccupyRoom(OccupyRequest request);

        /// <summary>
        /// Move specific inhabitant coupling (aka AgreementRequest) from romm
        /// </summary>
        Task UnOccupyRoom(UnOccupyRequest request);

        /// <summary>
        /// Remove all inhabitants from room
        /// </summary>
        Task UnOccupyRoomAll(UnOccupyAllRequest request);
    }

    public class OccupyRequest 
    {
        public int ProjectId { get; set; }
        public int RoomId { get; set; }
        public int AccommodationRequestId { get; set; }
    }

    public class UnOccupyRequest
    {
        public int ProjectId { get; set; }
        public int AccommodationRequestId { get; set; }
    }

    public class UnOccupyAllRequest
    {
    public int ProjectId { get; set; }
    public int RoomId { get; set; }
    }
}
