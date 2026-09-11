using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain.Test.AddClaim;

/// <summary>
/// Checks that every reason not to allow to send claim is translated to exception
/// </summary>
public class ExceptionTranslationTest
{
    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<AddClaimForbideReason>))]
    public void AllForbideReasonTranslatedToThrow(AddClaimForbideReason reason)
    {
        var claim = new Claim();
        var claimInfo = new UserClaimInfo(claim.GetId(), claim.ClaimStatus);
        var projectInfo = new MockedProject().ProjectInfo;
        _ = Should.Throw<JoinRpgBaseException>(() => ClaimValidator.ThrowForReason(ClaimForbiddenReason.For(reason), claimInfo, projectInfo));
    }
}
