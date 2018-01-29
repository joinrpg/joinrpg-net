using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using Microsoft.Practices.ObjectBuilder2;
using Newtonsoft.Json;

namespace JoinRpg.Web.Models.Accommodation
{

    public class AccRequestViewModel
    {
        public int Id { get; set; }

        [JsonIgnore]
        public int ProjectId { get; set; }

        [JsonIgnore]
        public int AccommodationTypeId { get; set; }

        [JsonIgnore]
        public int RoomId { get; set; }

        [JsonIgnore]
        public RoomViewModel Room { get; set; }

        [JsonIgnore]
        public IReadOnlyList<RequestParticipantViewModel> Participants { get; set; }

        public int Persons
            => Participants?.Count ?? 0;

        public string PersonsList
            => Participants.JoinStrings(", ", p => p.User.GetName());

        public object Instance
            => null;

        public AccRequestViewModel(AccommodationRequest entity)
        {
            Id = entity.Id;
            ProjectId = entity.ProjectId;
            AccommodationTypeId = entity.AccommodationTypeId;
            RoomId = entity.AccommodationId ?? 0;
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
