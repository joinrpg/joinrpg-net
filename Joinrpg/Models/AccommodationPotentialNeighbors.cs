using System;
using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
    public class AccommodationPotentialNeighbors
    {
        public int ClaimId { get; set; }
        public string ClaimName { get; set; }
        public string UserName { get; set; }
        public NeighborType Type { get; set; }
        public int? AccommodationRequestId { get; set; }

        public AccommodationPotentialNeighbors(Claim claim, NeighborType type)
        {
            ClaimId = claim.ClaimId;
            ClaimName = claim.Name;
            UserName = claim.Player.PrefferedName;
            Type = type;
            AccommodationRequestId = claim.AccommodationRequest_Id;
        }
    }

    public enum NeighborType
    {
        Current,
        WithSameType,
        NoRequest,
    }


    public class AccommodationPotentialNeighborsComparer : IEqualityComparer<AccommodationPotentialNeighbors>
    {

        public bool Equals(AccommodationPotentialNeighbors x, AccommodationPotentialNeighbors y)
        {
            //Check whether the objects are the same object. 
            if (Object.ReferenceEquals(x, y)) return true;

            //Check whether the products' properties are equal. 
            return x != null && y != null && x.ClaimId.Equals(y.ClaimId);
        }

        public int GetHashCode(AccommodationPotentialNeighbors obj)
        {
            //Get hash code for the Name field if it is not null. 
            var hashProductClaimName = obj.ClaimName == null ? 0 : obj.ClaimName.GetHashCode();
            var hashProductUserName = obj.UserName == null ? 0 : obj.UserName.GetHashCode();

            //Get hash code for the Code field. 
            var hashClaimId = obj.ClaimId.GetHashCode();

            //Calculate the hash code for the product. 
            return hashProductClaimName ^ hashProductUserName ^ hashClaimId;
        }
    }
}
