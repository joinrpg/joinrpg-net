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
            => Participants.JoinStrings(", ", p => p.UserName);

        public object Instance
            => null;

        // Payment status in percents (0 -- not paid, 100 -- completedly paid by everybody in group)
        public int PaymentStatus { get; }

        [JsonIgnore]
        public string PaymentStatusCssClass
            => PaymentStatus == 100 ? @"success"
                : (PaymentStatus >= 75 ? @"info"
                : (PaymentStatus >= 50 ? @"warning"
                : @"danger"));

        public AccRequestViewModel(AccommodationRequest entity)
        {
            Id = entity.Id;
            ProjectId = entity.ProjectId;
            AccommodationTypeId = entity.AccommodationTypeId;
            RoomId = entity.AccommodationId ?? 0;
            Participants = entity.Subjects.Select(c => new RequestParticipantViewModel(c)).ToList();
            PaymentStatus = 100 * Participants.Sum(p => p.Claim.ClaimPaidInFull() ? 1 : 0) / Participants.Count;
        }
    }

    public class RequestParticipantViewModel
    {
        public int UserId;
        public User User;
        public int ClaimId;
        public Claim Claim;

        public string UserName
            => User?.GetDisplayName() ?? "";

        public RequestParticipantViewModel(Claim claim)
        {
            ClaimId = claim.ClaimId;
            Claim = claim;
            UserId = claim.PlayerUserId;
            User = claim.Player;
        }
    }
}
