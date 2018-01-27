using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
    public interface IAccommodationService
    {
        Task<ProjectAccommodationType> RegisterNewAccommodationTypeAsync(ProjectAccommodationType newAccommodation);
        Task<ProjectAccommodation> RegisterNewProjectAccommodationAsync(ProjectAccommodation newProjectAccommodation);
        Task RemoveAccommodationType(int accommodationTypeId);
        Task RemoveProjectAccommodation(int projectAccommodationId);
        Task<IReadOnlyCollection<ProjectAccommodationType>> GetAccommodationForProject(int project);
        Task<ProjectAccommodationType> GetAccommodationByIdAsync(int accId);
        Task<ProjectAccommodation> GetProjectAccommodationByIdAsync(int accId);

    }
}
