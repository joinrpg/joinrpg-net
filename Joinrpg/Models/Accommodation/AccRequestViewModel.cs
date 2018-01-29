using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
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

        // Payment status in promille (0 -- not paid, 1000 -- completedly paid by everybody in group)
        public int PaymentStatus { get; }

        public AccRequestViewModel(AccommodationRequest entity)
        {
            Id = entity.Id;
            ProjectId = entity.ProjectId;
            AccommodationTypeId = entity.AccommodationTypeId;
            RoomId = entity.AccommodationId ?? 0;
            Participants = entity.Subjects.Select(c => new RequestParticipantViewModel(c)).ToList();
            PaymentStatus = Participants.Sum(p => p.Claim.ClaimPaidInFull() ? 1000 % Participants.Count : 0);
        }
    }

    public class RequestParticipantViewModel
    {
        public int UserId;
        public User User;
        public int ClaimId;
        public Claim Claim;

        public RequestParticipantViewModel(Claim claim)
        {
            ClaimId = claim.ClaimId;
            Claim = claim;
            UserId = claim.PlayerUserId;
            User = claim.Player;
        }
    }
}
