using JoinRpg.Helpers;
using JoinRpg.Web.Claims;

namespace JoinRpg.WebPortal.Models.Test;

public class ClaimStatusTooltipTest
{
    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<ClaimStatusView>))]
    public void TooltipIsNonEmptyForMasterAndPlayer(ClaimStatusView status)
    {
        var masterText = ClaimStatusTooltip.Build(status, denialStatus: null, isMaster: true);
        var playerText = ClaimStatusTooltip.Build(status, denialStatus: null, isMaster: false);

        masterText.ShouldNotBeNullOrWhiteSpace();
        playerText.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void DeclinedByMasterTooltipForMasterIncludesDenialReason()
    {
        var text = ClaimStatusTooltip.Build(ClaimStatusView.DeclinedByMaster, ClaimDenialStatusView.NotSuitable, isMaster: true);

        text.ShouldContain(ClaimDenialStatusView.NotSuitable.GetDisplayName());
    }
}
