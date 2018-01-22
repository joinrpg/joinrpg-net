using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
    public interface IAccommodationRepository
    {
        Task<IReadOnlyCollection<ProjectAccommodationType>> GetAccommodationForProject(int projectId);
    }
}
