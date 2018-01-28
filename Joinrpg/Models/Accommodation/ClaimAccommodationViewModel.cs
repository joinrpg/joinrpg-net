using System.Collections.Generic;
using JoinRpg.DataModel;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Models.Accommodation
{
    public class ClaimAccommodationViewModel
    {
        public int ClaimId { get; set; }
        public int ProjectId { get; set; }
        public IEnumerable<ProjectAccommodationType> AvailableAccommodationTypes { get; set; }
        public AccommodationRequest AccommodationRequest { get; set; }
        public bool AccommodationEnabled { get; set; }
    }
}
