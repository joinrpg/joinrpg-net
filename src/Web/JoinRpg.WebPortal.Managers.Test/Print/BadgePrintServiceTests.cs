using JoinRpg.Data.Interfaces.ProjectMetadata;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using JoinRpg.WebPortal.Managers.Print;

namespace JoinRpg.WebPortal.Managers.Test.Print;

/// <summary>
/// Сборка бейджей: строки шаблона заполняются значениями полей каждого персонажа.
/// </summary>
public class BadgePrintServiceTests
{
    private readonly MockedProject mock = new();
    private readonly List<ProjectBadgeTemplate> templates = [];

    private BadgePrintService CreateService()
        => new(new FakeBadgeTemplateRepository(templates),
            new FakeProjectMetadataRepository(mock),
            new FakeCharacterInfoRepository(mock),
            new FakeUserRepository(mock.TryGetUserInfo));

    private ProjectBadgeTemplateIdentification AddTemplate(bool isActive = true, params BadgeLine[] lines)
    {
        var id = new ProjectBadgeTemplateIdentification(mock.ProjectInfo.ProjectId, templates.Count + 1);
        templates.Add(new ProjectBadgeTemplate(id, "Игрок", new BadgeLayout(90, 55, null, lines), isActive));
        return id;
    }

    private static BadgeLine FieldLine(ProjectFieldInfo field) => new(field.Id.ProjectFieldId, null, null, 10, 8, 20, false);

    private static BadgeLine TextLine(string text) => new(null, text, null, 40, 8, 12, false);

    private CharacterIdentification CreateCharacter(string name, string quote)
    {
        var character = mock.CreateCharacter(name);
        MockedProject.AssignFieldValues(character, new FieldWithValue(mock.CharacterFieldInfo, quote));
        return mock.GetCharacterInfo(character).Id;
    }

    [Fact]
    public async Task Badges_FillFieldsPerCharacterInNameOrder()
    {
        var first = CreateCharacter("Яромир", "Цитата Яромира");
        var second = CreateCharacter("Агния", "Цитата Агнии");
        var templateId = AddTemplate(lines: [FieldLine(mock.CharacterFieldInfo), TextLine("Каэр Морхен")]);

        var model = (await CreateService().GetBadges(templateId, [first, second])).ShouldNotBeNull();

        model.Badges.Select(b => b[0].Text).ShouldBe(["Цитата Агнии", "Цитата Яромира"]);
        model.Badges.ShouldAllBe(b => b[1].Text == "Каэр Морхен");
        model.Warnings.ShouldBeEmpty();
    }

    // При выбранном поле статический текст — только пример для превью: печатается значение поля.
    [Fact]
    public async Task FieldLineWithSampleText_PrintsFieldValue()
    {
        var character = CreateCharacter("Первый", "Цитата первого");
        var templateId = AddTemplate(lines: [FieldLine(mock.CharacterFieldInfo) with { StaticText = "Пример для превью" }]);

        var model = (await CreateService().GetBadges(templateId, [character])).ShouldNotBeNull();

        model.Badges.ShouldHaveSingleItem().ShouldHaveSingleItem().Text.ShouldBe("Цитата первого");
    }

    // Пример — только для превью: при пустом поле у персонажа строка печатается пустой, а не примером.
    [Fact]
    public async Task FieldLineWithSampleText_EmptyField_PrintsNothing()
    {
        var character = CreateCharacter("Первый", "");
        var templateId = AddTemplate(lines: [FieldLine(mock.CharacterFieldInfo) with { StaticText = "Пример для превью" }]);

        var model = (await CreateService().GetBadges(templateId, [character])).ShouldNotBeNull();

        model.Badges.ShouldHaveSingleItem().ShouldHaveSingleItem().Text.ShouldBe("");
    }

    [Fact]
    public async Task BottomAnchor_ReachesComponent()
    {
        var character = CreateCharacter("Первый", "раз");
        var templateId = AddTemplate(lines: [TextLine("при помощи joinrpg.ru") with { OffsetMm = 5, Anchor = BadgeLineAnchor.Bottom }]);

        var line = (await CreateService().GetBadges(templateId, [character])).ShouldNotBeNull().Badges.Single().Single();

        line.FromBottom.ShouldBeTrue();
        line.OffsetMm.ShouldBe(5);
    }

    [Fact]
    public async Task Badges_SkipInactiveCharacters()
    {
        var active = CreateCharacter("Активный", "раз");
        var inactiveCharacter = mock.CreateCharacter("Удалённый");
        inactiveCharacter.IsActive = false;
        var templateId = AddTemplate(lines: [TextLine("Текст")]);

        var model = (await CreateService().GetBadges(templateId, [active, mock.GetCharacterInfo(inactiveCharacter).Id])).ShouldNotBeNull();

        model.Badges.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task DeletedField_GivesEmptyLineAndWarning()
    {
        var deleted = mock.AddField(f =>
        {
            f.FieldName = "Позывной";
            f.FieldType = ProjectFieldType.String;
            f.FieldBoundTo = FieldBoundTo.Character;
            f.IsActive = false;
        });
        // У поля осталось значение — агрегат его отдаёт, но печатать его нельзя.
        var characterEntity = mock.CreateCharacter("Первый");
        MockedProject.AssignFieldValues(characterEntity, new FieldWithValue(deleted, "Старый позывной"));
        var character = mock.GetCharacterInfo(characterEntity).Id;
        var templateId = AddTemplate(lines: [FieldLine(deleted)]);

        var model = (await CreateService().GetBadges(templateId, [character])).ShouldNotBeNull();

        model.Badges.ShouldHaveSingleItem().ShouldHaveSingleItem().Text.ShouldBe("");
        model.Warnings.ShouldHaveSingleItem().ShouldContain("Позывной");
    }

    [Fact]
    public async Task RemovedTemplate_GivesNull()
    {
        var templateId = AddTemplate(isActive: false);

        (await CreateService().GetBadges(templateId, [])).ShouldBeNull();
    }

    private class FakeBadgeTemplateRepository(List<ProjectBadgeTemplate> templates) : IProjectBadgeTemplateRepository
    {
        public Task<IReadOnlyCollection<ProjectBadgeTemplate>> GetForProjectAsync(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyCollection<ProjectBadgeTemplate>>(templates);

        public Task<ProjectBadgeTemplate?> GetByIdAsync(ProjectBadgeTemplateIdentification id)
            => Task.FromResult(templates.SingleOrDefault(t => t.Id == id));
    }
}
