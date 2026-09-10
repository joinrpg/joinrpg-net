using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Helpers;

namespace JoinRpg.DomainTypes.Characters.Claims;

public class ClaimAlreadyPresentException : JoinRpgBaseException
{
    public ClaimAlreadyPresentException() : base("Claim already present for this character or group.") { }
}

public class OnlyOneApprovedClaimException : JoinRpgBaseException
{
    public OnlyOneApprovedClaimException() : base("Approved claim already present for this player, and project allows only one character.") { }
}

public class ClaimTargetIsNotAcceptingClaims : JoinRpgBaseException
{
    public ClaimTargetIsNotAcceptingClaims() : base("This character or group does not accept claims.") { }
}

public class InsufficientContactsException() : JoinRpgBaseException("Для отправки заявки необходимы контакты");

public class ClaimWrongStatusException : JoinRpgProjectException
{
    public ClaimWrongStatusException(ClaimIdentification claimId, ClaimStatus currentStatus, IEnumerable<ClaimStatus> possible)
      : base(claimId.ProjectId, $"This operation can be performed only on claims with status {string.Join(", ", possible.Select(s => s.ToString()))}, but current status is {currentStatus}")
    {
        ClaimId = claimId;
    }

    public ClaimWrongStatusException(ClaimIdentification claimId, ClaimStatus currentStatus)
      : base(claimId.ProjectId, $"This operation can not be performed on claim with status = {currentStatus}.")
    {
        ClaimId = claimId;
    }

    public ClaimIdentification ClaimId { get; }
}
