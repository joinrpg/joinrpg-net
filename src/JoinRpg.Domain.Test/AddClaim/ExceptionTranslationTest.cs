using JoinRpg.DataModel;
using JoinRpg.TestHelpers;
using Shouldly;
using Xunit;

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
        _ = Should.Throw<JoinRpgBaseException>(() => ClaimSourceExtensions.ThrowForReason(reason, claim));
    }
}
