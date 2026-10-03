using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Problems.CharacterProblemFilters;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// Фильтр битых персонажей поверх доменного агрегата (ADR013): смотрит только на группы
/// персонажа вверх до корня.
/// </summary>
public class BrokenCharactersFilterTest
{
    private readonly MockedProject mock = new();
    private readonly BrokenCharactersFilter filter = new();

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
}
