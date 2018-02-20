using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using Microsoft.Practices.ObjectBuilder2;
using Newtonsoft.Json;

namespace JoinRpg.Web.Models.Accommodation
{

    public class AccRequestViewModel
    {
        public int Id { get; protected set; }

        [JsonIgnore]
        public int ProjectId { get; protected set; }

        [JsonIgnore]
        public int AccommodationTypeId { get; protected set; }

        public int RoomId { get; protected set; }

        [JsonIgnore]
        public RoomViewModel Room { get; set; }

        [JsonIgnore]
        public IReadOnlyList<RequestParticipantViewModel> Participants { get; protected set; }

        public int Persons
            => Participants?.Count ?? 0;

        public string PersonsList
            => Participants.JoinStrings(@", ", p => p.UserName);

        public object Instance
            => null;

        public int FeeTotal { get; protected set; }

        public int FeeToPay { get; protected set; }

        public string PaymentStatusCssClass { get; protected set; }

        public string PaymentStatusTitle
            => FeeToPay > 0 ? $@"Не оплачено {FeeToPay} из {FeeTotal}" : "Все оплачено полностью";

        public AccRequestViewModel(AccommodationRequest entity)
        {
            Id = entity.Id;
            ProjectId = entity.ProjectId;
            AccommodationTypeId = entity.AccommodationTypeId;
            RoomId = entity.AccommodationId ?? 0;
            Participants = entity.Subjects.Select(c => new RequestParticipantViewModel(c)).ToList();
            FeeTotal = Participants.Sum(p => p.FeeTotal);
            FeeToPay = Participants.Sum(p => p.FeeToPay);
            FeeToPay = FeeToPay > 0 ? FeeToPay : 0; // if FeeToPay < 0 we have overpaid
            int percent = FeeToPay > 0 ? 100 * FeeToPay / FeeTotal : 100;
            PaymentStatusCssClass = percent == 100 ? @"success" : (percent <= 25 ? @"warning" : @"danger");
        }
    }

    public class RequestParticipantViewModel
    {
        public int UserId;
        public User User;
        public int ClaimId;
        public Claim Claim;

        public int FeeToPay;
        public int FeeTotal;

        public string UserName
            => User?.GetDisplayName() ?? "";

        public RequestParticipantViewModel(Claim claim)
        {
            ClaimId = claim.ClaimId;
            Claim = claim;
            UserId = claim.PlayerUserId;
            User = claim.Player;
            FeeTotal = Claim.ClaimTotalFee();
            FeeToPay = Claim.ClaimFeeDue();
        }
    }
}
