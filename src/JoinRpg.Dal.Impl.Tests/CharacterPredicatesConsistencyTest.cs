using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Tests;

/// <summary>
/// Тест на согласованность из issue #4766: <see cref="CharacterPredicates.IsAvailable"/> —
/// грубый SQL-префильтр, а точный ответ на вопрос "можно ли сюда заявиться" даёт доменный движок
/// правил (<see cref="ClaimAcceptOrMoveValidationExtensions.ValidateIfCanAddClaim"/>).
/// </summary>
/// <remarks>
/// Инвариант ровно такой, какой описан в issue: префильтр не должен быть строже правил. Он вправе
/// пропускать лишнее (например, слот без мест — это известное расхождение, см.
/// <c>JoinRpg.WebPortal.Managers.CharacterGroupList.CharacterListViewService</c>, который досеивает
/// результат доменными правилами), но не вправе отсекать то, что правила разрешают — иначе персонаж
/// молча пропадёт из списка ещё до того, как до него доберутся доменные правила.
/// </remarks>
public class CharacterPredicatesConsistencyTest
{
    private readonly MockedProject _mock = new();

    private bool SqlPrefilterAllows(Character character)
        => CharacterPredicates.IsAvailable(_mock.ProjectInfo.ProjectId).Compile()(character);

    private bool DomainRulesAllow(Character character)
        => character.ValidateIfCanAddClaim(userInfo: null, _mock.ProjectInfo, ClaimOperation.AddByMaster).Count == 0;

    private void AssertPrefilterNotStricterThanRules(Character character)
    {
        if (DomainRulesAllow(character))
        {
            SqlPrefilterAllows(character).ShouldBeTrue(
                $"Доменные правила разрешают заявку на '{character.CharacterName}', а SQL-префильтр её отсёк бы — расхождение из issue #4766.");
        }
    }

    [Fact]
    public void OrdinaryCharacter_PrefilterMatchesRules()
        => AssertPrefilterNotStricterThanRules(_mock.Character);

    [Fact]
    public void InactiveCharacter_PrefilterMatchesRules()
    {
        var character = _mock.CreateCharacter("inactive");
        character.IsActive = false;

        AssertPrefilterNotStricterThanRules(character);
    }

    [Fact]
    public void NpcCharacter_PrefilterMatchesRules()
    {
        var character = _mock.CreateCharacter("npc");
        character.CharacterType = CharacterType.NonPlayer;

        AssertPrefilterNotStricterThanRules(character);
    }

    [Fact]
    public void BusyCharacterWithApprovedClaim_PrefilterMatchesRules()
    {
        var character = _mock.CreateCharacter("busy-approved");
        _ = _mock.CreateApprovedClaim(character, _mock.Player);

        AssertPrefilterNotStricterThanRules(character);
    }

    [Fact]
    public void BusyCharacterWithCheckedInClaim_PrefilterMatchesRules()
    {
        var character = _mock.CreateCharacter("busy-checkedin");
        _ = _mock.CreateCheckedInClaim(character, _mock.Player);

        AssertPrefilterNotStricterThanRules(character);
    }

    [Fact]
    public void SlotWithFreePlaces_PrefilterMatchesRules()
    {
        var slot = _mock.CreateCharacter("slot-with-places");
        slot.CharacterType = CharacterType.Slot;
        slot.CharacterSlotLimit = 3;

        AssertPrefilterNotStricterThanRules(slot);
    }

    /// <summary>
    /// Заведомое расхождение из issue: SQL-префильтр про лимит слота не знает и пропускает
    /// исчерпанный слот, доменные правила его отсекают. Префильтру можно быть шире правил — это не
    /// нарушение инварианта, фиксируем явно, чтобы не потерять из виду.
    /// </summary>
    [Fact]
    public void ExhaustedSlot_PrefilterIsWiderThanRules_KnownDivergence()
    {
        var slot = _mock.CreateCharacter("slot-exhausted");
        slot.CharacterType = CharacterType.Slot;
        slot.CharacterSlotLimit = 0;

        DomainRulesAllow(slot).ShouldBeFalse();
        SqlPrefilterAllows(slot).ShouldBeTrue();
    }

    /// <summary>
    /// Регресс на риск из issue #4766: легаси-колонка <c>Character.IsAcceptingClaims</c> (см.
    /// <c>[Obsolete]</c> у неё) у старых записей могла разъехаться с
    /// <see cref="Character.CharacterType"/>. Опасное направление — когда она ошибочно выставлена
    /// в <c>false</c> у обычного игрового персонажа: доменные правила такого персонажа не отсекают,
    /// а старый SQL-префильтр, глядя только на легаси-флаг, отсёк бы его молча.
    /// </summary>
    /// <remarks>
    /// Поэтому <see cref="CharacterPredicates.IsAvailable"/> смотрит на
    /// <see cref="Character.CharacterType"/>, а не на легаси-флаг — здесь тест намеренно
    /// выставляет флаг в заведомо рассинхронизированное значение и проверяет, что префильтр его
    /// игнорирует.
    /// </remarks>
    [Fact]
    public void StaleLegacyFlag_PlayerCharacterMarkedAsNotAccepting_PrefilterMustNotHideIt()
    {
        var character = _mock.CreateCharacter("stale-legacy-flag");
        character.CharacterType = CharacterType.Player;
#pragma warning disable CS0618 // намеренно рассинхронизированный легаси-флаг — так выглядели старые записи
        character.IsAcceptingClaims = false;
#pragma warning restore CS0618

        AssertPrefilterNotStricterThanRules(character);
    }
}
