namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Контракт нормализации поиска. Её повторяет normalizeSearch в JoinRpg.Common.WebComponents.lib.module.js
/// (поиск в статических формах) — при изменении этих случаев надо поправить и JS.
/// </summary>
public class TypedSelectSearchTest
{
    [Theory]
    [InlineData("Алёна", "алена")]
    [InlineData("ЙОРГЕН", "иорген")]
    [InlineData("  Иван   Петров  ", "иван петров")]
    [InlineData("Иван Петров", "иван петров")] // неразрывный пробел
    [InlineData("Иван\tПетров\n", "иван петров")]
    [InlineData("Café", "cafe")]
    [InlineData("Дракон 🐉", "дракон 🐉")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalize(string? text, string expected) => TypedSelectSearch.Normalize(text).ShouldBe(expected);

    /// <summary>Имя, обрезанное посреди эмодзи, не должно ронять рендер селектора, а с ним и страницу.</summary>
    [Fact]
    public void Normalize_UnpairedSurrogate_DoesNotThrow()
        => TypedSelectSearch.Normalize("Дракон \uD83D").ShouldBe("дракон \uD83D");

    [Theory]
    [InlineData("алена", true)]
    [InlineData("АЛЁНА", true)]
    [InlineData("клан алена", true)] // слова в любом порядке
    [InlineData("алена борис", false)] // каждое слово должно найтись
    [InlineData("", true)]
    public void Matches(string query, bool expected)
        => TypedSelectSearch.Matches(TypedSelectSearch.Normalize("Алёна Северный клан"), query).ShouldBe(expected);
}
