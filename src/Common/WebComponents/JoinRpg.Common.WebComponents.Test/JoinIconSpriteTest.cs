namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Спрайт иконок должен соответствовать таблице <see cref="JoinIconType"/> и исходникам набора.
/// </summary>
/// <remarks>
/// Если тест упал после добавления иконки — перегенерируйте спрайт, запустив тесты
/// с переменной окружения <c>JOINRPG_UPDATE_SPRITE=1</c>.
/// </remarks>
public class JoinIconSpriteTest
{
    private const string UpdateEnvironmentVariable = "JOINRPG_UPDATE_SPRITE";

    [Fact]
    public void SpriteIsUpToDate()
    {
        var repositoryRoot = RepositoryLocator.FindRoot();
        var spritePath = Path.Combine(repositoryRoot, JoinIconSpriteBuilder.SpriteRelativePath);
        var expected = JoinIconSpriteBuilder.Build(
            Path.Combine(repositoryRoot, JoinIconSpriteBuilder.SourceRelativePath));

        if (Environment.GetEnvironmentVariable(UpdateEnvironmentVariable) == "1")
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(spritePath)!);
            File.WriteAllText(spritePath, expected);
            return;
        }

        File.Exists(spritePath).ShouldBeTrue($"Спрайт не собран: {JoinIconSpriteBuilder.SpriteRelativePath}");
        File.ReadAllText(spritePath).Replace("\r\n", "\n").ShouldBe(
            expected,
            $"Спрайт устарел. Перегенерируйте: {UpdateEnvironmentVariable}=1 dotnet test");
    }

    [Fact]
    public void EveryIconTypeHasSymbolInSprite()
    {
        var sprite = File.ReadAllText(
            Path.Combine(RepositoryLocator.FindRoot(), JoinIconSpriteBuilder.SpriteRelativePath));

        foreach (var icon in Enum.GetValues<JoinIconType>())
        {
            var symbolId = JoinIconDefinitions.Get(icon).IconName;
            sprite.ShouldContain($"id=\"{symbolId}\"", customMessage: $"Иконка {icon} не попала в спрайт");
        }
    }
}
