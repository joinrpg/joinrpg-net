using JoinRpg.DomainTypes.ProjectMetadata.Badges;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

public class BadgeLayoutRulesTest
{
    private static readonly BadgeLine fieldLine = new(1, null, null, OffsetMm: 10, MinFontPt: 8, MaxFontPt: 20, Bold: false);

    private static BadgeLayout Layout(params BadgeLine[] lines) => new(90, 55, null, lines);

    [Fact]
    public void ValidTemplateHasNoErrors()
        => BadgeLayoutRules.GetErrors("Игрок", Layout(fieldLine, fieldLine with { ProjectFieldId = null, StaticText = "Текст" })).ShouldBeEmpty();

    [Fact]
    public void EmptyNameIsError() => BadgeLayoutRules.GetErrors(" ", Layout()).ShouldHaveSingleItem();

    [Theory]
    [InlineData(BadgeLayoutRules.MinSizeMm - 1, 55)]
    [InlineData(BadgeLayoutRules.MaxWidthMm + 1, 55)]
    [InlineData(90, BadgeLayoutRules.MaxHeightMm + 1)]
    public void SizeOutOfA4IsError(int width, int height)
        => BadgeLayoutRules.GetErrors("Игрок", Layout() with { WidthMm = width, HeightMm = height }).ShouldHaveSingleItem();

    [Theory]
    [InlineData("ftp://example.com/bg.png")]
    [InlineData("bg.png")]
    public void NonHttpBackgroundIsError(string url)
        => BadgeLayoutRules.GetErrors("Игрок", Layout() with { BackgroundUrl = new Uri(url, UriKind.RelativeOrAbsolute) }).ShouldHaveSingleItem();

    [Fact]
    public void HttpsBackgroundIsNotError()
        => BadgeLayoutRules.GetErrors("Игрок", Layout() with { BackgroundUrl = new Uri("https://example.com/bg.png") }).ShouldBeEmpty();

    // При выбранном поле статический текст — пример для превью, а не второй источник.
    [Fact]
    public void LineWithFieldAndSampleTextIsValid()
        => BadgeLayoutRules.GetErrors("Игрок", Layout(fieldLine with { StaticText = "Цирилла" })).ShouldBeEmpty();

    [Fact]
    public void BottomAnchoredLineWithinBadgeIsValid()
        => BadgeLayoutRules.GetErrors("Игрок", Layout(fieldLine with { OffsetMm = 5, Anchor = BadgeLineAnchor.Bottom })).ShouldBeEmpty();

    // Строка общего шаблона без привязки к полю допустима только там; в шаблоне проекта источник обязателен.
    [Fact]
    public void LineWithoutSourceIsError()
        => BadgeLayoutRules.GetErrors("Игрок", Layout(fieldLine with { ProjectFieldId = null, SlotLabel = "Имя" })).ShouldHaveSingleItem();

    [Fact]
    public void MinFontAboveMaxIsError()
        => BadgeLayoutRules.GetErrors("Игрок", Layout(fieldLine with { MinFontPt = 30 })).ShouldHaveSingleItem();

    [Fact]
    public void LongNameIsError()
        => BadgeLayoutRules.GetErrors(new string('я', BadgeLayoutRules.MaxNameLength + 1), Layout()).ShouldHaveSingleItem();

    [Fact]
    public void LongBackgroundIsError()
        => BadgeLayoutRules.GetErrors("Игрок", Layout() with { BackgroundUrl = new Uri("https://example.com/" + new string('a', BadgeLayoutRules.MaxBackgroundUrlLength)) }).ShouldHaveSingleItem();

    [Theory]
    [InlineData(-1, 8, 20)]
    [InlineData(10, BadgeLayoutRules.MinFontPt - 1, 20)]
    [InlineData(10, 8, BadgeLayoutRules.MaxFontPt + 1)]
    public void LineOutOfRangeIsError(int topMm, int minFont, int maxFont)
        => BadgeLayoutRules.GetErrors("Игрок", Layout(fieldLine with { OffsetMm = topMm, MinFontPt = minFont, MaxFontPt = maxFont })).ShouldHaveSingleItem();

    [Fact]
    public void LineBelowBadgeIsError()
        => BadgeLayoutRules.GetErrors("Игрок", Layout(fieldLine with { OffsetMm = 55 })).ShouldHaveSingleItem();

    // A4 минус отступы 10 мм — 190×277 мм под бейджи.
    [Theory]
    [InlineData(90, 55, 10)]   // 2 в ряд × 5 рядов
    [InlineData(190, 277, 1)]  // самый большой допустимый — один на лист
    [InlineData(54, 86, 9)]    // вертикальная карточка: 3 × 3
    public void BadgesPerSheet_FitsIntoA4WithMargins(int width, int height, int expected)
        => BadgeLayoutRules.BadgesPerSheet(width, height).ShouldBe(expected);

    [Fact]
    public void MaxSize_IsA4MinusMargins()
        => (BadgeLayoutRules.MaxWidthMm, BadgeLayoutRules.MaxHeightMm).ShouldBe((190, 277));
}
