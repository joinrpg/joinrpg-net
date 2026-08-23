using System.Collections.Frozen;

namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Как рисуется конкретная иконка.
/// </summary>
/// <param name="IconName">Имя иконки в наборе Tabler (оно же имя файла в Icons/tabler и id символа в спрайте).</param>
/// <param name="Variation">
/// Цвет, неотделимый от смысла иконки (например красная звёздочка обязательного поля).
/// </param>
internal readonly record struct JoinIconDefinition(
    string IconName,
    VariationStyleEnum? Variation = null)
{
    public static implicit operator JoinIconDefinition(string iconName) => new(iconName);
}

/// <summary>
/// Единственное место, где иконка Joinrpg связывается с конкретным иконочным набором.
/// Набор — <see href="https://tabler.io/icons">Tabler Icons</see> (MIT), исходники лежат в Icons/tabler,
/// из них собирается спрайт со всеми иконками (см. JoinIconSpriteBuilder).
/// </summary>
/// <remarks>
/// Имена иконок набора не должны встречаться больше нигде в репозитории — это проверяется тестом.
/// Чтобы добавить иконку: заведите значение <see cref="JoinIconType"/>, положите svg-файл Tabler
/// в Icons/tabler, добавьте строчку сюда и пересоберите спрайт (см. JoinIconSpriteTest).
/// </remarks>
internal static class JoinIconDefinitions
{
    internal static readonly FrozenDictionary<JoinIconType, JoinIconDefinition> All =
        new Dictionary<JoinIconType, JoinIconDefinition>
        {
            { JoinIconType.Add, "plus" },
            { JoinIconType.Ok, "check" },
            { JoinIconType.Delete, "trash" },
            { JoinIconType.Download, "cloud-download" },
            { JoinIconType.Export, "download" },
            { JoinIconType.Refresh, "refresh" },
            { JoinIconType.Hide, "circle-x" },
            { JoinIconType.Closed, "circle-x" },
            { JoinIconType.Print, "printer" },
            { JoinIconType.Email, "mail" },
            { JoinIconType.Publish, "share" },
            { JoinIconType.Copy, "copy" },
            { JoinIconType.MoveUp, "arrow-up" },
            { JoinIconType.MoveDown, "arrow-down" },
            { JoinIconType.Edit, "pencil" },
            { JoinIconType.Setup, "settings" },
            { JoinIconType.Tools, "tool" },
            { JoinIconType.Claim, "file" },
            { JoinIconType.Hot, "flame" },
            { JoinIconType.User, "user" },
            { JoinIconType.Money, "coin" },
            { JoinIconType.Sort, "arrows-sort" },
            { JoinIconType.SortByAlphabet, "sort-ascending-letters" },
            { JoinIconType.Progress, "hourglass" },
            { JoinIconType.Info, "info-circle" },
            { JoinIconType.Subscribe, "pin" },
            { JoinIconType.Warning, "alert-triangle" },
            { JoinIconType.Alert, "alert-circle" },
            { JoinIconType.Remove, "x" },
            { JoinIconType.Accommodation, "bed" },
            { JoinIconType.Evict, "arrows-maximize" },
            { JoinIconType.EvictAll, "maximize" },
            { JoinIconType.FeePaid, "thumb-up" },
            { JoinIconType.Telegram, "brand-telegram" },
            { JoinIconType.Help, "help-circle" },
            { JoinIconType.Phone, "phone" },
            { JoinIconType.Approve, new("check", VariationStyleEnum.Success) },
            { JoinIconType.Decline, new("ban", VariationStyleEnum.Danger) },
            { JoinIconType.Back, "arrow-left" },
            { JoinIconType.Search, "search" },
            { JoinIconType.Calendar, "calendar" },
            { JoinIconType.Visible, "eye" },
            { JoinIconType.Hidden, "eye-off" },
            { JoinIconType.CheckboxChecked, "checkbox" },
            { JoinIconType.CheckboxUnchecked, "square" },
            { JoinIconType.MenuVertical, "dots-vertical" },
            { JoinIconType.MenuHorizontal, "dots" },
            { JoinIconType.HasAccess, "circle-check" },
            { JoinIconType.NoAccess, "circle-x" },
            { JoinIconType.Move, "transfer" },
            { JoinIconType.CheckIn, "tent" },
            { JoinIconType.GoToFirst, "player-skip-back" },
            { JoinIconType.GoToPrev, "player-track-prev" },
            { JoinIconType.GoToNext, "player-track-next" },

            { JoinIconType.MandatoryStar, new("asterisk", VariationStyleEnum.Danger) },
            { JoinIconType.RecommendedStar, new("asterisk", VariationStyleEnum.Info) },
            { JoinIconType.Close, "x" },
            { JoinIconType.DialogQuestion, "question-mark" },
            { JoinIconType.DialogInfo, "info-circle" },
            { JoinIconType.DialogSuccess, "heart" },
            { JoinIconType.DialogAlert, "alert-triangle" },
            { JoinIconType.DialogFail, "x" },
            { JoinIconType.Forbidden, "ban" },

            { JoinIconType.List, "list" },
            { JoinIconType.Award, "award" },
            { JoinIconType.Achievement, "trophy" },
            { JoinIconType.Project, "layout-kanban" },
            { JoinIconType.Home, "home" },

            { JoinIconType.Vk, "brand-vk" },
        }.ToFrozenDictionary();

    internal static JoinIconDefinition Get(JoinIconType icon)
        => All.TryGetValue(icon, out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(nameof(icon), icon, "Для иконки не задано определение");
}
