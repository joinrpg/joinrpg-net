using JoinRpg.Portal.Menu;

namespace JoinRpg.Portal.Test.Menu;

public class PageTitleTest
{
    [Theory]
    [InlineData("Сетка ролей", "Тестовая игра", "Сетка ролей — Тестовая игра — JoinRpg")]
    [InlineData("Поддержка", null, "Поддержка — JoinRpg")]
    [InlineData(null, "Тестовая игра", "Тестовая игра — JoinRpg")]
    [InlineData(null, null, "JoinRpg")]
    public void BuildTitle_SkipsMissingParts(string? pageTitle, string? projectName, string expected)
        => PageTitleViewComponent.BuildTitle(pageTitle, projectName).ShouldBe(expected);
}
