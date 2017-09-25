using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
    public interface IAccomodationService
    {
        Task<ProjectAccomodationType> RegisterNewAccomodationTypeAsync(ProjectAccomodationType newAccomodation);
        Task RemoveAccomodationType(int accomodationTypeId);
        Task<IReadOnlyCollection<ProjectAccomodationType>> GetAccomodationForProject(int project);
        Task<ProjectAccomodationType> GetAccomodationByIdAsync(int accId);

    }
}
