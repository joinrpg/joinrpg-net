using JoinRpg.Helpers;
using JoinRpg.Services.Drp.MasterExports;
using JoinRpg.Web.Claims;

namespace JoinRpg.Services.Drp.Test.MasterExports;

/// <summary>
/// Разбор держит свою копию названий статусов, чтобы сервис не ссылался на UI-библиотеку.
/// Тест стережёт, что копия совпадает с тем, что пишет выгрузка (<see cref="ClaimStatusView"/>).
/// </summary>
public class MasterExportStatusNamesTest
{
    public static TheoryData<ClaimStatus> AllStatuses => [.. Enum.GetValues<ClaimStatus>()];

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void StatusNameMatchesClaimStatusView(ClaimStatus status)
    {
        var view = (ClaimStatusView)status;
        Enum.IsDefined(view).ShouldBeTrue();
        view.ToString().ShouldBe(status.ToString());

        MasterExportParser.StatusDisplayNames[status].ShouldBe(view.GetDisplayName());
    }
}
