using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Problems;
using JoinRpg.Domain.Problems.CharacterProblemFilters;
using JoinRpg.Domain.Problems.CommonProblemFilters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// Фильтр битых персонажей поверх доменного агрегата (ADR013): смотрит только на группы
/// персонажа вверх до корня. Считает проблемы и для заявок: проблемы утверждённой заявки —
/// надмножество проблем персонажа.
/// </summary>
public class BrokenCharactersFilterTest
{
    private readonly MockedProject mock = new();
    private readonly BrokenCharactersFilter filter = new();

    private readonly CharacterProblemValidator characterValidator = new(
        [new BrokenCharactersFilter()],
        [new FieldNotSetFilter(), new InActiveVariantsFilter()]);

    private readonly ClaimProblemValidator claimValidator = new(
        [new BrokenCharactersFilter()],
        [new FieldNotSetFilter(), new InActiveVariantsFilter()]);

    [Fact]
    public void CharacterInRootGroupHasNoProblems()
    {
        // Персонаж мока лежит в корневой группе — у неё родителей нет по определению,
        // и битой она не считается.
        var problems = filter.GetProblems(mock.GetCharacterInfo(mock.Character));

        problems.ShouldBeEmpty();
    }

    [Fact]
    public void CharacterWithoutAnyGroupIsFatal()
    {
        var character = mock.CreateCharacter("Бездомный");
        character.ParentCharacterGroupIds = [];

        var problems = filter.GetProblems(mock.GetCharacterInfo(character)).ToArray();

        problems.Length.ShouldBe(1);
        problems[0].ProblemType.ShouldBe(ClaimProblemType.NoParentGroup);
        problems[0].Severity.ShouldBe(ProblemSeverity.Fatal);
    }

    [Fact]
    public void CharacterInGroupWithoutParentsIsFatal()
    {
        // mock.Group — не корневая группа и без родителей: ровно та поломка, которую ищет фильтр.
        var character = mock.CreateCharacter("В битой группе");
        character.ParentCharacterGroupIds = [mock.Group.CharacterGroupId];

        var problems = filter.GetProblems(mock.GetCharacterInfo(character)).ToArray();

        problems.Length.ShouldBe(1);
        problems[0].ProblemType.ShouldBe(ClaimProblemType.GroupIsBroken);
        problems[0].Severity.ShouldBe(ProblemSeverity.Fatal);
        problems[0].ExtraInfo.ShouldBe(mock.Group.CharacterGroupName);
    }

    // Вторую ветку GroupIsBroken — группа, которая сама себе родитель — тестом не закрыть:
    // сборка метаданных проекта (CharacterGroupDictionaryBuilder) на таком дереве уходит
    // в бесконечную рекурсию, то есть до фильтра дело не доходит. Это не регрессия миграции:
    // EF-путь строил те же метаданные.

    [Fact]
    public void DeletedGroupDoesNotCountAsParent()
    {
        var group = mock.CreateCharacterGroup();
        group.ParentCharacterGroupIds = [mock.Project.RootGroup.CharacterGroupId];
        group.IsActive = false;
        var character = mock.CreateCharacter("В удалённой группе");
        character.ParentCharacterGroupIds = [group.CharacterGroupId];
        mock.ReInitProjectInfo();

        var problems = filter.GetProblems(mock.GetCharacterInfo(character)).ToArray();

        // Удалённая группа не считается, а её родитель — корень — в путь наверх всё равно попадает,
        // поэтому «нет групп» здесь не возникает.
        problems.ShouldBeEmpty();
    }

    [Fact]
    public void ClaimOnCharacterInBrokenGroupSeesGroupIsBroken()
    {
        // CreateClaim — заявка не утверждена: структурные проблемы видны всем заявкам.
        var character = mock.CreateCharacter("В битой группе");
        character.ParentCharacterGroupIds = [mock.Group.CharacterGroupId];
        _ = mock.CreateClaim(character, mock.Player);

        var problems = ((IClaimProblemFilter)filter).GetProblems(MakeClaimContext(character)).ToArray();

        problems.ShouldHaveSingleItem();
        problems[0].ProblemType.ShouldBe(ClaimProblemType.GroupIsBroken);
        problems[0].Severity.ShouldBe(ProblemSeverity.Fatal);
        problems[0].ExtraInfo.ShouldBe(mock.Group.CharacterGroupName);
    }

    [Fact]
    public void ClaimOnCharacterWithoutGroupSeesNoParentGroup()
    {
        var character = mock.CreateCharacter("Бездомный");
        character.ParentCharacterGroupIds = [];
        _ = mock.CreateClaim(character, mock.Player);

        var problems = ((IClaimProblemFilter)filter).GetProblems(MakeClaimContext(character)).ToArray();

        problems.ShouldHaveSingleItem();
        problems[0].ProblemType.ShouldBe(ClaimProblemType.NoParentGroup);
        problems[0].Severity.ShouldBe(ProblemSeverity.Fatal);
    }

    /// <summary>
    /// Инвариант: проблемы утверждённой заявки — надмножество проблем персонажа. Проверяем на
    /// сломанной группе и обязательном поле слоя заявки: персонажная сторона видит обе проблемы.
    /// </summary>
    [Fact]
    public void ApprovedClaimProblemsAreSupersetOfCharacterProblems()
    {
        var character = mock.CreateCharacter("В битой группе");
        character.ParentCharacterGroupIds = [mock.Group.CharacterGroupId];
        _ = mock.CreateApprovedClaim(character, mock.Player);
        _ = mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.String;
            f.FieldBoundTo = FieldBoundTo.Claim;
            f.MandatoryStatus = MandatoryStatus.Required;
            f.ValidForNpc = true;
        });

        var characterInfo = mock.GetCharacterInfo(character);

        var characterProblems = characterValidator.Validate(characterInfo)
            .Select(p => p.ProblemType).ToArray();
        var claim = characterInfo.Claims.Single(c => c.IsApproved);
        var claimProblems = claimValidator.Validate(new ClaimInfo(new ClaimInCharacter(characterInfo, claim), mock.PlayerInfo))
            .Select(p => p.ProblemType).ToArray();

        // Страж от вакуусного надмножества: персонаж обязан иметь ровно эти две проблемы.
        characterProblems.ShouldBe([ClaimProblemType.GroupIsBroken, ClaimProblemType.FieldIsEmpty], ignoreOrder: true);
        characterProblems.ShouldAllBe(type => claimProblems.Contains(type));
    }

    private ClaimInfo MakeClaimContext(Character character)
    {
        var characterInfo = mock.GetCharacterInfo(character);
        return new ClaimInfo(new ClaimInCharacter(characterInfo, characterInfo.Claims.Single()), mock.PlayerInfo);
    }
}
