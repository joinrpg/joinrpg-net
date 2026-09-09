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
