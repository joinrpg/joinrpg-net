namespace JoinRpg.DomainTypes.ProjectMetadata.Badges;

/// <summary>Шаблон бейджа нарушает <see cref="BadgeLayoutRules"/>; в сообщении — все ошибки</summary>
public class BadgeTemplateInvalidException(ProjectIdentification projectId, IReadOnlyCollection<string> errors)
    : JoinRpgProjectException(projectId, string.Join("; ", errors))
{
    public IReadOnlyCollection<string> Errors { get; } = errors;
}

/// <summary>
/// Ограничения на шаблон бейджа. Бейджи печатаются на A4, поэтому бейдж должен влезть в лист с полями.
/// </summary>
public static class BadgeLayoutRules
{
    public const int MinSizeMm = 10;
    public const int MaxWidthMm = 200;
    public const int MaxHeightMm = 287;
    public const int MinFontPt = 4;
    public const int MaxFontPt = 200;
    public const int MaxNameLength = 100;
    public const int MaxBackgroundUrlLength = 1024;

    /// <summary>Бросает <see cref="BadgeTemplateInvalidException"/>, если шаблон некорректен</summary>
    public static void EnsureValid(ProjectIdentification projectId, string name, BadgeLayout layout)
    {
        var errors = GetErrors(name, layout).ToList();
        if (errors.Count > 0)
        {
            throw new BadgeTemplateInvalidException(projectId, errors);
        }
    }

    /// <summary>
    /// Ошибки шаблона бейджа проекта. Пусто — шаблон корректен. Строка шаблона проекта обязана брать
    /// значение ровно из одного источника: поля проекта или статического текста.
    /// </summary>
    // TODO[Localize]
    public static IEnumerable<string> GetErrors(string name, BadgeLayout layout)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            yield return "Не указано название шаблона";
        }
        else if (name.Length > MaxNameLength)
        {
            yield return $"Название шаблона длиннее {MaxNameLength} символов";
        }

        if (layout.WidthMm is < MinSizeMm or > MaxWidthMm)
        {
            yield return $"Ширина бейджа должна быть от {MinSizeMm} до {MaxWidthMm} мм";
        }

        if (layout.HeightMm is < MinSizeMm or > MaxHeightMm)
        {
            yield return $"Высота бейджа должна быть от {MinSizeMm} до {MaxHeightMm} мм";
        }

        if (!string.IsNullOrWhiteSpace(layout.BackgroundUrl) && layout.BackgroundUrl is var url
            && (url.Length > MaxBackgroundUrlLength
                || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)))
        {
            yield return "Ссылка на фон должна быть полным адресом http:// или https://";
        }

        for (var i = 0; i < layout.Lines.Count; i++)
        {
            var line = layout.Lines[i];
            var number = i + 1;
            var hasField = line.ProjectFieldId is not null;
            var hasText = !string.IsNullOrWhiteSpace(line.StaticText);
            if (hasField == hasText)
            {
                yield return $"Строка {number}: нужно выбрать либо поле проекта, либо статический текст";
            }

            if (line.TopMm < 0 || line.TopMm >= layout.HeightMm)
            {
                yield return $"Строка {number}: отступ сверху должен быть от 0 до высоты бейджа";
            }

            if (line.MinFontPt < MinFontPt || line.MaxFontPt > MaxFontPt)
            {
                yield return $"Строка {number}: размер шрифта должен быть от {MinFontPt} до {MaxFontPt} пт";
            }

            if (line.MinFontPt > line.MaxFontPt)
            {
                yield return $"Строка {number}: минимальный шрифт больше максимального";
            }
        }
    }
}
