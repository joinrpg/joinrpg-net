using JoinRpg.Helpers;
using JoinRpg.Services.Drp.MasterExports;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Services.Drp.Test.MasterExports;

/// <summary>
/// Разбор держит свою копию значений «Занят?», чтобы сервис не ссылался на UI-библиотеку.
/// Тест стережёт, что копия совпадает с тем, что пишет выгрузка (<see cref="CharacterBusyStatusView"/>).
/// </summary>
public class MasterExportBusyStatusNamesTest
{
    public static TheoryData<CharacterBusyStatusView> AllStatuses => [.. Enum.GetValues<CharacterBusyStatusView>()];

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void BusyStatusNameMatchesView(CharacterBusyStatusView status)
    {
        var copy = MasterExportParser.BusyStatuses.Where(s => s.Member == status.ToString()).ShouldHaveSingleItem();

        copy.DisplayName.ShouldBe(status.GetDisplayName());
    }

    [Fact]
    public void NoExtraBusyStatuses()
        => MasterExportParser.BusyStatuses.Select(s => s.Member).ShouldBe(
            Enum.GetNames<CharacterBusyStatusView>(), ignoreOrder: true);
}
