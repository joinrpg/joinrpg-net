using System.Collections.Generic;
using System.Linq;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.Accommodation
{

    public class AccRequestViewModel
    {
        public int ProjectId { get; set; }
        public int AccommodationTypeId { get; set; }
        public int RoomId { get; set; }
        public RoomViewModel Room { get; set; }

        public IReadOnlyList<RequestParticipantViewModel> Participants { get; set; }

        public int ParticipantsCount
            => Participants?.Count ?? 0;

        public AccRequestViewModel(AccommodationRequest entity)
        {
            ProjectId = entity.ProjectId;
            AccommodationTypeId = entity.AccommodationTypeId;
            Participants = entity.Subjects.Select(c => new RequestParticipantViewModel(c)).ToList();
        }
    }

    public class RequestParticipantViewModel
    {
        public int UserId;
        public User User;
        public int ClaimId;

        public RequestParticipantViewModel(Claim claim)
        {
            ClaimId = claim.ClaimId;
            UserId = claim.PlayerUserId;
            User = claim.Player;
        }
    }
}
