using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// <see cref="CharacterBusyStatusBadge"/> показывает «Неизвестно» только для
/// <see cref="CharacterBusyStatusView.Unknown"/>. Раньше персонаж с активной, но не принятой
/// заявкой (<see cref="CharacterBusyStatusView.Discussed"/>) попадал в ту же ветку и в списке
/// персонажей и кабинете капитана выглядел красным «Неизвестно».
/// </summary>
public class CharacterBusyStatusBadgeTest
{
    private const string UnknownText = "Неизвестно";

    private static IRenderedComponent<CharacterBusyStatusBadge> Render(BunitContext ctx, CharacterBusyStatusView status)
        => ctx.Render<CharacterBusyStatusBadge>(p => p
            .Add(x => x.CharacterBusyStatus, status)
            .Add(x => x.SlotCount, null));

    [Fact]
    public void Discussed_ShouldRenderDiscussed()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, CharacterBusyStatusView.Discussed);

        cut.Markup.ShouldContain("Обсуждается");
        cut.Markup.ShouldNotContain(UnknownText);
    }

    /// <summary>Все значения, кроме <see cref="CharacterBusyStatusView.Unknown"/>, — новое значение попадёт сюда само.</summary>
    public static TheoryData<CharacterBusyStatusView> KnownStatuses
        => [.. Enum.GetValues<CharacterBusyStatusView>().Where(s => s != CharacterBusyStatusView.Unknown)];

    [Theory]
    [MemberData(nameof(KnownStatuses))]
    public void KnownStatus_ShouldNotRenderUnknown(CharacterBusyStatusView status)
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, status);

        cut.Markup.ShouldNotContain(UnknownText);
    }
}
