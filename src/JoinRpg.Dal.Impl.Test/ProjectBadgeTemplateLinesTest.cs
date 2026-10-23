using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using Shouldly;
using Xunit;
using ProjectBadgeTemplate = JoinRpg.DataModel.ProjectBadgeTemplate;

namespace JoinRpg.Dal.Impl.Test;

/// <summary>Формат хранения строк бейджа в колонке <see cref="ProjectBadgeTemplate.LinesJson"/></summary>
public class ProjectBadgeTemplateLinesTest
{
    private static readonly BadgeLine[] lines =
    [
        new(ProjectFieldId: 123, StaticText: null, SlotLabel: "Имя персонажа", OffsetMm: 20, MinFontPt: 10, MaxFontPt: 28, Bold: true),
        new(ProjectFieldId: null, StaticText: "Ведьмак: Каэр Морхен", SlotLabel: null, OffsetMm: 5, MinFontPt: 8, MaxFontPt: 12, Bold: false, Anchor: BadgeLineAnchor.Bottom),
    ];

    [Fact]
    public void ShouldRoundTrip()
    {
        var entity = new ProjectBadgeTemplate { Lines = lines };

        entity.Lines.ShouldBe(lines);
    }

    // Формат лежит в БД: переименование свойства в BadgeLine молча обнулит его у уже сохранённых шаблонов.
    [Fact]
    public void ShouldReadStoredFormat()
    {
        var entity = new ProjectBadgeTemplate
        {
            LinesJson = """
                [{"projectFieldId":123,"staticText":null,"slotLabel":"Имя персонажа","offsetMm":20,"minFontPt":10,"maxFontPt":28,"bold":true},
                 {"projectFieldId":null,"staticText":"Ведьмак: Каэр Морхен","slotLabel":null,"offsetMm":5,"minFontPt":8,"maxFontPt":12,"bold":false,"anchor":"Bottom"}]
                """,
        };

        entity.Lines.ShouldBe(lines);
    }

    // Привязка пишется словом, а не числом: читается в БД и не зависит от порядка значений в enum.
    [Fact]
    public void ShouldStoreAnchorAsString()
        => new ProjectBadgeTemplate { Lines = lines }.LinesJson.ShouldContain("\"anchor\":\"Bottom\"");

    [Fact]
    public void ShouldStoreCyrillicReadable()
        => new ProjectBadgeTemplate { Lines = lines }.LinesJson.ShouldContain("Каэр Морхен");

    [Fact]
    public void NewTemplateHasNoLines() => new ProjectBadgeTemplate().Lines.ShouldBeEmpty();
}
