namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Иконочный набор должен быть скрыт за <see cref="JoinIconType"/>.
/// Если этот тест упал — вы добавили иконку строкой; используйте компонент <c>JoinIcon</c>
/// (в .razor) или тег-хелпер <c>&lt;join-icon /&gt;</c> (в .cshtml).
/// </summary>
public class IconsAreEncapsulatedTest
{
    private static readonly string ForbiddenMarker = JoinIconDefinitions.GlyphFontCssClass;

    /// <summary>
    /// Файлы, где строка иконочного набора допустима.
    /// </summary>
    private static readonly string[] AllowedFiles =
    [
        // Единственное место, где иконка связывается с иконочным набором.
        "src/Common/WebComponents/JoinRpg.Common.WebComponents/Icons/JoinIconDefinitions.cs",
        // Разметка строится в js — компонент туда не дотянется, см. отдельную задачу.
        "src/JoinRpg.Portal/wwwroot/Scripts/rooms.js",
    ];

    private static readonly string[] ScannedExtensions = [".razor", ".cshtml", ".cs", ".js"];

    [Fact]
    public void IconSetNameIsNotUsedOutsideOfDefinitions()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceRoot = Path.Combine(repositoryRoot, "src");

        var offenders = Directory
            .EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
            .Where(path => ScannedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .Where(path => !path.Contains("/wwwroot/lib/", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains("/obj/", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains("/bin/", StringComparison.OrdinalIgnoreCase))
            .Where(path => !AllowedFiles.Contains(path, StringComparer.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(Path.Combine(repositoryRoot, path))
                .Contains(ForbiddenMarker, StringComparison.OrdinalIgnoreCase))
            .Order()
            .ToList();

        offenders.ShouldBeEmpty(
            $"Имя иконочного набора должно встречаться только в JoinIconDefinitions. Используйте JoinIcon / <join-icon />. Нарушители:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Joinrpg.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Не найден корень репозитория (Joinrpg.slnx)");
    }
}
