using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.Accommodation
{

    public class AccRequestViewModel
    {
        public int ProjectId { get; set; }
        public int AccommodationTypeId { get; set; }
        public int RoomId { get; set; }
        public RoomViewModel Room { get; set; }

        public IEnumerable<RequestParticipantViewModel> Participants { get; set; }

        public int ParticipantsCount { get; private set; }

        public AccRequestViewModel(AccommodationRequest entity)
        {
            ProjectId = entity.ProjectId;
            AccommodationTypeId = entity.AccommodationTypeId;
            Participants = entity.Subjects.Select(c =>
            {
                ParticipantsCount++;
                return new RequestParticipantViewModel()
                {
                    UserId = c.PlayerUserId,
                    User = c.Player,
                    ClaimId = c.ClaimId
                };
            });
        }
    }

    public class RequestParticipantViewModel
    {
        public int UserId;
        public User User;
        public int ClaimId;
    }
}
