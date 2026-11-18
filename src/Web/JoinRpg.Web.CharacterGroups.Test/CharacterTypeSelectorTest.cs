using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Web.CharacterGroups.Test;

/// <summary>
/// Форма персонажа постит <see cref="CharacterTypeInfo"/> в MVC, и биндинг собирает его по
/// параметрам конструктора. Чего форма не запостила, то биндинг подставит по умолчанию — для
/// «игрок может подать заявку» (#3303) это «можно». Поэтому проверяется именно то, что уходит
/// в POST: первое значение поля, как его берёт MVC.
/// </summary>
public class CharacterTypeSelectorTest
{
    private const string AcceptingClaimsField = "CharacterTypeInfo.IsAcceptingClaims";
    private const string IsHotField = "CharacterTypeInfo.IsHot";

    private static IRenderedComponent<CharacterTypeSelector> Render(
        BunitContext ctx, CharacterTypeInfo model, bool hasApprovedClaim = false, bool isDefaultTemplate = false)
        => ctx.Render<CharacterTypeSelector>(p => p
            .Add(x => x.InitialModel, model)
            .Add(x => x.HasApprovedClaim, hasApprovedClaim)
            .Add(x => x.IsDefaultTemplate, isDefaultTemplate));

    /// <summary>
    /// Значение, которое получит биндинг: первое из отправляемых браузером (hidden — всегда,
    /// checkbox — только отмеченный и не заблокированный), или <c>null</c>, если поле не постится.
    /// Не зависит от того, в каком порядке <c>CheckboxInput</c> рендерит свои input'ы.
    /// </summary>
    private static string? Posted(IRenderedComponent<CharacterTypeSelector> cut, string name)
        => cut.FindAll($"input[name='{name}']")
            .Where(input => input.GetAttribute("type") switch
            {
                "hidden" => true,
                "checkbox" => input.HasAttribute("checked") && !input.HasAttribute("disabled"),
                _ => false,
            })
            .Select(input => input.GetAttribute("value") ?? "on")
            .FirstOrDefault();

    private static string? Posted(
        CharacterTypeInfo model, string name, bool hasApprovedClaim = false, bool isDefaultTemplate = false)
    {
        using var ctx = new BunitContext();
        return Posted(Render(ctx, model, hasApprovedClaim, isDefaultTemplate), name);
    }

    private static CharacterTypeInfo Model(CharacterType type, bool isAcceptingClaims, bool isHot = false)
        => new(type, isHot, SlotLimit: null, SlotName: null, CharacterVisibility.Public, isAcceptingClaims);

    [Theory]
    [InlineData(CharacterType.Player)]
    [InlineData(CharacterType.Slot)]
    public void ClosedForPlayersIsPostedAsFalse(CharacterType type)
        => Posted(Model(type, isAcceptingClaims: false), AcceptingClaimsField).ShouldBe("false");

    [Fact]
    public void OpenForPlayersIsPostedAsTrue()
        => Posted(Model(CharacterType.Player, isAcceptingClaims: true), AcceptingClaimsField).ShouldBe("true");

    /// <summary>
    /// У занятой роли «Горячая роль» не показывается, а флаг — показывается: иначе правка
    /// персонажа с утверждённой заявкой молча открыла бы его для заявок игроков.
    /// </summary>
    [Fact]
    public void FlagSurvivesEditOfBusyCharacter()
        => Posted(Model(CharacterType.Player, isAcceptingClaims: false), AcceptingClaimsField, hasApprovedClaim: true)
            .ShouldBe("false");

    // Горячая роль при закрытых для игроков заявках запрещена инвариантом CharacterTypeInfo.
    [Fact]
    public void HotIsNotPostedWhenClosedForPlayers()
        => Posted(Model(CharacterType.Player, isAcceptingClaims: false), IsHotField).ShouldBeNull();

    [Fact]
    public void HotIsPostedWhenOpenForPlayers()
        => Posted(Model(CharacterType.Player, isAcceptingClaims: true, isHot: true), IsHotField).ShouldBe("true");

    // На шаблон по умолчанию ведёт кнопка «Подать заявку в проект» — закрыть его нельзя,
    // а оказавшийся закрытым форма откроет обратно.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DefaultTemplateIsAlwaysPostedOpen(bool storedFlag)
        => Posted(Model(CharacterType.Slot, isAcceptingClaims: storedFlag), AcceptingClaimsField, isDefaultTemplate: true)
            .ShouldBe("true");

    // Главный сценарий: мастер снял галочку — уходит «нельзя», а «Горячая роль» больше не постится.
    [Fact]
    public void UncheckingClosesForPlayersAndDropsHot()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, Model(CharacterType.Player, isAcceptingClaims: true, isHot: true));

        cut.Find($"input[type=checkbox][name='{AcceptingClaimsField}']").Change(false);

        Posted(cut, AcceptingClaimsField).ShouldBe("false");
        Posted(cut, IsHotField).ShouldBeNull();
    }

    // У NPC флаг снят, но при переводе в игроки заявки по умолчанию открыты.
    [Fact]
    public void NpcTurnedIntoPlayerIsOpenForPlayers()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, Model(CharacterType.NonPlayer, isAcceptingClaims: false));

        cut.Find("input[type=radio][value='Player']").Change("Player");

        Posted(cut, AcceptingClaimsField).ShouldBe("true");
    }
}
