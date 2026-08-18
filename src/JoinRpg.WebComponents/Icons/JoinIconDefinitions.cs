using System.Collections.Frozen;

namespace JoinRpg.WebComponents;

/// <summary>
/// Способ отрисовки иконки.
/// </summary>
internal enum JoinIconKind
{
    /// <summary>Символ иконочного шрифта.</summary>
    Glyph,
    /// <summary>Юникодный символ.</summary>
    Text,
    /// <summary>Картинка.</summary>
    Image,
}

/// <summary>
/// Как рисуется конкретная иконка.
/// </summary>
/// <param name="Kind">Способ отрисовки.</param>
/// <param name="Value">Css-класс глифа, текст символа или путь к картинке — в зависимости от <paramref name="Kind"/>.</param>
/// <param name="Variation">
/// Цвет, неотделимый от смысла иконки (например красная звёздочка обязательного поля).
/// Переопределяется параметром <c>Variation</c> компонента.
/// </param>
internal readonly record struct JoinIconDefinition(
    JoinIconKind Kind,
    string Value,
    VariationStyleEnum? Variation = null)
{
    public static JoinIconDefinition Glyph(string glyphName, VariationStyleEnum? variation = null)
        => new(JoinIconKind.Glyph, "glyphicon-" + glyphName, variation);

    public static JoinIconDefinition Text(string content, VariationStyleEnum? variation = null)
        => new(JoinIconKind.Text, content, variation);

    public static JoinIconDefinition Image(string fileName)
        => new(JoinIconKind.Image, "/_content/JoinRpg.WebComponents/images/" + fileName);
}

/// <summary>
/// Единственное место, где иконка Joinrpg связывается с конкретным иконочным набором.
/// Строка «glyphicon» не должна встречаться больше нигде в репозитории — это проверяется тестом.
/// </summary>
internal static class JoinIconDefinitions
{
    /// <summary>Css-класс иконочного шрифта Bootstrap 3.</summary>
    internal const string GlyphFontCssClass = "glyphicon";

    internal static readonly FrozenDictionary<JoinIconType, JoinIconDefinition> All =
        new Dictionary<JoinIconType, JoinIconDefinition>
        {
            { JoinIconType.Add, JoinIconDefinition.Glyph("plus") },
            { JoinIconType.Ok, JoinIconDefinition.Glyph("ok") },
            { JoinIconType.Delete, JoinIconDefinition.Glyph("trash") },
            { JoinIconType.Download, JoinIconDefinition.Glyph("cloud-download") },
            { JoinIconType.Export, JoinIconDefinition.Glyph("download") },
            { JoinIconType.Refresh, JoinIconDefinition.Glyph("refresh") },
            { JoinIconType.Hide, JoinIconDefinition.Glyph("remove-sign") },
            { JoinIconType.Closed, JoinIconDefinition.Glyph("remove-sign") },
            { JoinIconType.Print, JoinIconDefinition.Glyph("print") },
            { JoinIconType.Email, JoinIconDefinition.Glyph("envelope") },
            { JoinIconType.Publish, JoinIconDefinition.Glyph("share-alt") },
            { JoinIconType.Copy, JoinIconDefinition.Glyph("duplicate") },
            { JoinIconType.MoveUp, JoinIconDefinition.Glyph("arrow-up") },
            { JoinIconType.MoveDown, JoinIconDefinition.Glyph("arrow-down") },
            { JoinIconType.Edit, JoinIconDefinition.Glyph("pencil") },
            { JoinIconType.Setup, JoinIconDefinition.Glyph("cog") },
            { JoinIconType.Tools, JoinIconDefinition.Glyph("wrench") },
            { JoinIconType.Claim, JoinIconDefinition.Glyph("file") },
            { JoinIconType.Hot, JoinIconDefinition.Glyph("fire") },
            { JoinIconType.User, JoinIconDefinition.Glyph("user") },
            { JoinIconType.Money, JoinIconDefinition.Glyph("usd") },
            { JoinIconType.Sort, JoinIconDefinition.Glyph("sort") },
            { JoinIconType.SortByAlphabet, JoinIconDefinition.Glyph("sort-by-alphabet") },
            { JoinIconType.Progress, JoinIconDefinition.Glyph("hourglass") },
            { JoinIconType.Info, JoinIconDefinition.Glyph("info-sign") },
            { JoinIconType.Subscribe, JoinIconDefinition.Glyph("pushpin") },
            { JoinIconType.Warning, JoinIconDefinition.Glyph("warning-sign") },
            { JoinIconType.Alert, JoinIconDefinition.Glyph("alert") },
            { JoinIconType.Remove, JoinIconDefinition.Glyph("remove") },
            { JoinIconType.Accommodation, JoinIconDefinition.Glyph("bed") },
            { JoinIconType.Evict, JoinIconDefinition.Glyph("resize-full") },
            { JoinIconType.EvictAll, JoinIconDefinition.Glyph("fullscreen") },
            { JoinIconType.FeePaid, JoinIconDefinition.Glyph("thumbs-up") },
            { JoinIconType.Telegram, JoinIconDefinition.Glyph("send") },
            { JoinIconType.Help, JoinIconDefinition.Glyph("question-sign") },
            { JoinIconType.Phone, JoinIconDefinition.Glyph("phone") },
            { JoinIconType.Approve, JoinIconDefinition.Glyph("check", VariationStyleEnum.Success) },
            { JoinIconType.Decline, JoinIconDefinition.Glyph("ban-circle", VariationStyleEnum.Danger) },
            { JoinIconType.Back, JoinIconDefinition.Glyph("arrow-left") },
            { JoinIconType.Search, JoinIconDefinition.Glyph("search") },
            { JoinIconType.Calendar, JoinIconDefinition.Glyph("calendar") },
            { JoinIconType.Visible, JoinIconDefinition.Glyph("eye-open") },
            { JoinIconType.Hidden, JoinIconDefinition.Glyph("eye-close") },
            { JoinIconType.CheckboxChecked, JoinIconDefinition.Glyph("check") },
            { JoinIconType.CheckboxUnchecked, JoinIconDefinition.Glyph("unchecked") },
            { JoinIconType.MenuVertical, JoinIconDefinition.Glyph("option-vertical") },
            { JoinIconType.MenuHorizontal, JoinIconDefinition.Glyph("option-horizontal") },
            { JoinIconType.HasAccess, JoinIconDefinition.Glyph("ok-sign") },
            { JoinIconType.NoAccess, JoinIconDefinition.Glyph("remove-sign") },
            { JoinIconType.Move, JoinIconDefinition.Glyph("transfer") },
            { JoinIconType.CheckIn, JoinIconDefinition.Glyph("tree-conifer") },
            { JoinIconType.GoToFirst, JoinIconDefinition.Glyph("fast-backward") },
            { JoinIconType.GoToPrev, JoinIconDefinition.Glyph("step-backward") },
            { JoinIconType.GoToNext, JoinIconDefinition.Glyph("forward") },

            { JoinIconType.MandatoryStar, JoinIconDefinition.Text("*", VariationStyleEnum.Danger) },
            { JoinIconType.RecommendedStar, JoinIconDefinition.Text("*", VariationStyleEnum.Info) },
            { JoinIconType.Close, JoinIconDefinition.Text("×") },
            { JoinIconType.DialogQuestion, JoinIconDefinition.Text("?") },
            { JoinIconType.DialogInfo, JoinIconDefinition.Text("i") },
            { JoinIconType.DialogSuccess, JoinIconDefinition.Text("♥") },
            { JoinIconType.DialogAlert, JoinIconDefinition.Text("!") },
            { JoinIconType.DialogFail, JoinIconDefinition.Text("×") },

            { JoinIconType.Vk, JoinIconDefinition.Image("logo-vk.svg") },
        }.ToFrozenDictionary();

    internal static JoinIconDefinition Get(JoinIconType icon)
        => All.TryGetValue(icon, out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(nameof(icon), icon, "Для иконки не задано определение");
}
