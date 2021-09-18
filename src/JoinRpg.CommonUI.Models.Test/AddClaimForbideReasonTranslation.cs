using System;
using System.Linq;
using JoinRpg.Domain;
using JoinRpg.TestHelpers;
using Shouldly;
using Xunit;

namespace JoinRpg.CommonUI.Models.Test
{
    public class AddClaimForbideReasonTranslation
    {
        [Theory]
        [MemberData(nameof(AddClaimForbideReasons))]
        public void AllTranslated(AddClaimForbideReason reason) => Should.NotThrow(() => reason.ToViewModel());

        [Fact]
        public void TranslatedToDistinct()
        {
            Enum.GetValues<AddClaimForbideReason>()
                .Select(x => x.ToViewModel()).ShouldBeUnique();
        }


        // ReSharper disable once MemberCanBePrivate.Global xUnit requirements
        public static TheoryData<AddClaimForbideReason> AddClaimForbideReasons =>
            EnumerationTestHelper.GetTheoryDataForAllEnumValues<AddClaimForbideReason>();
    }
}
