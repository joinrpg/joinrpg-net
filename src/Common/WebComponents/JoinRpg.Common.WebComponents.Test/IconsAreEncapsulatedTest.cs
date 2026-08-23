namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Иконка в разметке может появиться только одним способом — через <see cref="JoinIconType"/>.
/// Если этот тест упал — вы добавили иконку в обход: строкой иконочного шрифта, символом или своим svg.
/// Используйте компонент <c>JoinIcon</c> (в .razor) или тег-хелпер <c>&lt;join-icon /&gt;</c> (в .cshtml).
/// </summary>
public class IconsAreEncapsulatedTest
{
    private const string ThisTest =
        "src/Common/WebComponents/JoinRpg.Common.WebComponents.Test/IconsAreEncapsulatedTest.cs";

    /// <summary>
    /// Запрещённая в разметке строка и файлы, где она всё же допустима.
    /// </summary>
    /// <param name="Marker">Строка, выдающая иконку в обход <see cref="JoinIconType"/>.</param>
    /// <param name="Explanation">Что делать, если тест упал на этом маркере.</param>
    /// <param name="AllowedFiles">Файлы-исключения.</param>
    private sealed record ForbiddenMarker(string Marker, string Explanation, params string[] AllowedFiles);

    private static readonly ForbiddenMarker[] ForbiddenMarkers =
    [
        new("glyphicon",
            "Иконочный шрифт Bootstrap 3 больше не используется — возьмите иконку из JoinIconType.",
            ThisTest),
        new("oi oi-",
            "Набор open-iconic из шаблона Blazor не используется — возьмите иконку из JoinIconType.",
            ThisTest),
        new("&times;",
            "Крестик — это JoinIconType.Close, а не символ в разметке.",
            ThisTest),
        new("join-icons.svg",
            "Адрес спрайта знают только JoinIconMarkup и join-obsolete-icons.js.",
            ThisTest,
            "src/Common/WebComponents/JoinRpg.Common.WebComponents/Icons/JoinIconMarkup.cs",
            "src/Common/WebComponents/JoinRpg.Common.WebComponents/wwwroot/join-obsolete-icons.js",
            "src/Common/WebComponents/JoinRpg.Common.WebComponents.Test/JoinIconSpriteBuilder.cs"),
    ];

    private static readonly string[] ScannedExtensions = [".razor", ".cshtml", ".cs", ".js", ".css", ".html"];

    /// <summary>Папки, за которые мы не отвечаем: вендорные библиотеки и результаты сборки.</summary>
    private static readonly string[] SkippedFolders =
        ["/wwwroot/lib/", "/wwwroot/twitter-bootstrap/", "/obj/", "/bin/", "/Icons/tabler/"];

    [Fact]
    public void IconsAreOnlyRenderedByJoinIcon()
    {
        var repositoryRoot = RepositoryLocator.FindRoot();

        var sources = Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "src"), "*", SearchOption.AllDirectories)
            .Where(path => ScannedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .Where(path => !SkippedFolders.Any(folder => path.Contains(folder, StringComparison.OrdinalIgnoreCase)))
            .Select(path => (Path: path, Text: File.ReadAllText(Path.Combine(repositoryRoot, path))))
            .ToList();

        var offenders = ForbiddenMarkers
            .SelectMany(marker => sources
                .Where(source => !marker.AllowedFiles.Contains(source.Path, StringComparer.OrdinalIgnoreCase))
                .Where(source => source.Text.Contains(marker.Marker, StringComparison.OrdinalIgnoreCase))
                .Select(source => $"{source.Path}: «{marker.Marker}» — {marker.Explanation}"))
            .Order()
            .ToList();

        offenders.ShouldBeEmpty(
            $"Иконки рисуются только через JoinIcon / <join-icon />. Нарушители:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }
}
