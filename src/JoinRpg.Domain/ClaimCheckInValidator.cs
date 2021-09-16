using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
    public class ClaimCheckInValidator

    {
        private readonly Claim claim;

        public ClaimCheckInValidator([NotNull] Claim claim) => this.claim = claim ?? throw new ArgumentNullException(nameof(claim));

        public int FeeDue => claim.ClaimFeeDue();

        public bool NotCheckedInAlready => claim.CheckInDate == null &&
                                           claim.ClaimStatus != Claim.Status.CheckedIn;

        public bool IsApproved => claim.ClaimStatus == Claim.Status.Approved;

        public IReadOnlyCollection<FieldRelatedProblem> NotFilledFields => claim.GetProblems()
          .OfType<FieldRelatedProblem>().ToList();

        public bool CanCheckInInPrinciple => NotCheckedInAlready && IsApproved &&
                                             !NotFilledFields.Any();

        public bool CanCheckInNow => CanCheckInInPrinciple && FeeDue <= 0;
    }
}
