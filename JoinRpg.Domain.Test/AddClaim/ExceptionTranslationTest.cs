using JoinRpg.TestHelpers;
using Shouldly;
using Xunit;

namespace JoinRpg.Domain.Test.AddClaim
{
    /// <summary>
    /// Checks that every reason not to allow to send claim is translated to exception
    /// </summary>
    public class ExceptionTranslationTest
    {
        [Theory]
        [MemberData(nameof(AddClaimForbideReasons))]
        public void AllForbideReasonTranslatedToThrow(AddClaimForbideReason reason)
        {
            Should.Throw<JoinRpgBaseException>(() => ClaimSourceExtensions.ThrowForReason(reason));
        }

        // ReSharper disable once MemberCanBePrivate.Global xUnit requirements
        public static TheoryData<AddClaimForbideReason> AddClaimForbideReasons =>
            EnumerationTestHelper.GetTheoryDataForAllEnumValues<AddClaimForbideReason>();
    }
}
