using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
    public interface IAccommodationRepository
    {
        Task<IReadOnlyCollection<ProjectAccommodationType>> GetAccommodationForProject(int projectId);
        Task<IReadOnlyCollection<ProjectAccommodationType>> GetPlayerSelectableAccommodationForProject(int projectId);

        Task<IReadOnlyCollection<ClaimAccommodationInfoRow>> GetClaimAccommodationReport(
            int project);

    }

    public class ClaimAccommodationInfoRow
    {
        public int ClaimId { get; set; }
        public string AccomodationType { get; set; }
        public string RoomName { get; set; }
       public User User { get; set; }
    }
}
