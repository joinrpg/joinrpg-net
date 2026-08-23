using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Собирает спрайт иконок из исходных svg-файлов Tabler.
/// </summary>
/// <remarks>
/// Живёт в тестах, а не в самой библиотеке: спрайт собирается один раз при добавлении иконки,
/// приложению этот код не нужен. Проверяет и обновляет спрайт <see cref="JoinIconSpriteTest"/>.
/// </remarks>
internal static class JoinIconSpriteBuilder
{
    /// <summary>Папка с исходниками иконок, относительно корня репозитория.</summary>
    internal const string SourceRelativePath =
        "src/Common/WebComponents/JoinRpg.Common.WebComponents/Icons/tabler";

    /// <summary>Собранный спрайт, относительно корня репозитория.</summary>
    internal const string SpriteRelativePath =
        "src/Common/WebComponents/JoinRpg.Common.WebComponents/wwwroot/icons/join-icons.svg";

    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>Имена иконок набора, которые нужны хотя бы одному <see cref="JoinIconType"/>.</summary>
    internal static IReadOnlyList<string> RequiredIconNames { get; } =
        [.. JoinIconDefinitions.All.Values
            .Select(definition => definition.IconName)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

    /// <summary>
    /// Содержимое спрайта: по символу на каждую иконку из <see cref="RequiredIconNames"/>.
    /// </summary>
    /// <param name="sourceFolder">Папка с исходными svg-файлами Tabler.</param>
    internal static string Build(string sourceFolder)
    {
        var sprite = new XElement(Svg + "svg",
            new XAttribute("xmlns", Svg.NamespaceName),
            new XAttribute("style", "display: none"),
            new XComment(" Tabler Icons (MIT), https://tabler.io/icons. Файл собран JoinIconSpriteBuilder — не редактировать руками. "),
            RequiredIconNames.Select(iconName => BuildSymbol(sourceFolder, iconName)));

        return Serialize(sprite);
    }

    private static XElement BuildSymbol(string sourceFolder, string iconName)
    {
        var sourcePath = Path.Combine(sourceFolder, iconName + ".svg");
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException(
                $"Нет исходника иконки «{iconName}». Скачайте её из набора Tabler в {SourceRelativePath}.",
                sourcePath);
        }

        var source = XDocument.Parse(File.ReadAllText(sourcePath)).Root!;

        // width/height не нужны: размер иконки задаёт css на месте использования.
        var attributes = source.Attributes()
            .Where(attribute => attribute.Name.LocalName is not ("width" or "height"))
            .Where(attribute => !attribute.IsNamespaceDeclaration)
            .Select(attribute => new XAttribute(attribute.Name, attribute.Value));

        return new XElement(Svg + "symbol",
            new XAttribute("id", iconName),
            attributes,
            source.Elements().Where(IsVisibleShape).Select(shape => new XElement(shape)));
    }

    /// <summary>
    /// Прозрачный квадрат-подложка на весь холст в спрайте не нужен.
    /// </summary>
    private static bool IsVisibleShape(XElement shape)
        => shape.Attribute("d")?.Value != "M0 0h24v24H0z";

    private static string Serialize(XElement sprite)
    {
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true,
            IndentChars = "  ",
            // Перевод строки фиксируем, чтобы спрайт не зависел от того, на какой ОС его собрали.
            NewLineChars = "\n",
        };

        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(builder, settings))
        {
            sprite.Save(writer);
        }
        return builder.Append('\n').ToString();
    }
}
