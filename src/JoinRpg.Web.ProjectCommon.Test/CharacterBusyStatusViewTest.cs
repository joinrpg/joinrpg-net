using JoinRpg.Helpers;

namespace JoinRpg.Web.ProjectCommon.Test;

/// <summary>
/// Выгрузка списка персонажей пишет статус через <c>GetDisplayName()</c>. Без
/// <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/> у значения в таблицу
/// попадало бы имя члена enum — «Slot», «HotVacancy».
/// </summary>
public class CharacterBusyStatusViewTest
{
    public static TheoryData<CharacterBusyStatusView> AllStatuses => [.. Enum.GetValues<CharacterBusyStatusView>()];

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void EveryStatusHasDisplayName(CharacterBusyStatusView status)
        => status.GetDisplayName().ShouldNotBe(status.ToString());
}
