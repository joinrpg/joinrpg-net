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
        Task<ProjectAccommodationType> RegisterNewAccommodationTypeAsync(ProjectAccommodationType newAccommodation);

        Task RemoveAccommodationType(int accommodationTypeId);

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

        Task<IReadOnlyCollection<ProjectAccommodationType>> GetAccommodationForProject(int project);

        Task<ProjectAccommodationType> GetAccommodationByIdAsync(int accId);

        Task<ProjectAccommodation> GetProjectAccommodationByIdAsync(int accId);

        

        Task OccupyRoom(OccupyRequest request);

        Task UnOccupyRoom(UnOccupyRequest request);
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
}
