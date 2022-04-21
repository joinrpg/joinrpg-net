using JoinRpg.Domain;
using JoinRpg.TestHelpers;
using Shouldly;
using Xunit;

namespace JoinRpg.CommonUI.Models.Test;

public class AddClaimForbideReasonTranslation
{
    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<AddClaimForbideReason>))]
    public void AllTranslated(AddClaimForbideReason reason) => Should.NotThrow(() => reason.ToViewModel());

    [Fact]
    public void TranslatedToDistinct()
    {
        Enum.GetValues<AddClaimForbideReason>()
            .Select(x => x.ToViewModel()).ShouldBeUnique();
    }
}
