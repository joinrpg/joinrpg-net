using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
    public interface IAccomodationRepository
    {
        Task<IReadOnlyCollection<ProjectAccomodationType>> GetAccomodationForProject(int projectId);
    }
}
