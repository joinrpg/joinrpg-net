using JoinRpg.DomainTypes.ProjectMetadata.Badges;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

public class BadgeLinesJsonTest
{
    private static readonly BadgeLine[] lines =
    [
        new(ProjectFieldId: 123, StaticText: null, SlotLabel: "Имя персонажа", TopMm: 20, MinFontPt: 10, MaxFontPt: 28, Bold: true),
        new(ProjectFieldId: null, StaticText: "Ведьмак: Каэр Морхен", SlotLabel: null, TopMm: 5, MinFontPt: 8, MaxFontPt: 12, Bold: false),
    ];

    [Fact]
    public void ShouldRoundTrip()
        => BadgeLinesJson.Deserialize(BadgeLinesJson.Serialize(lines)).ShouldBe(lines);

    // Формат лежит в БД: переименование свойства в BadgeLine молча обнулит его у уже сохранённых шаблонов.
    [Fact]
    public void ShouldReadStoredFormat()
    {
        const string stored = """
            [{"projectFieldId":123,"staticText":null,"slotLabel":"Имя персонажа","topMm":20,"minFontPt":10,"maxFontPt":28,"bold":true},
             {"projectFieldId":null,"staticText":"Ведьмак: Каэр Морхен","slotLabel":null,"topMm":5,"minFontPt":8,"maxFontPt":12,"bold":false}]
            """;

        BadgeLinesJson.Deserialize(stored).ShouldBe(lines);
    }

    [Fact]
    public void EmptyArrayShouldGiveNoLines() => BadgeLinesJson.Deserialize("[]").ShouldBeEmpty();
}
