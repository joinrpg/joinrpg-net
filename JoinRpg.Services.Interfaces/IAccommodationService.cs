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

        /// <summary>
        /// Adds new room or list of rooms
        /// </summary>
        Task RegisterNewProjectAccommodationAsync(ProjectAccommodation newProjectAccommodation);

        Task RemoveAccommodationType(int accommodationTypeId);

        Task RemoveProjectAccommodation(int projectAccommodationId);

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
