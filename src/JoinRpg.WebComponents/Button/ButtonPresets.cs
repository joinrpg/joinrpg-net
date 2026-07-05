using System.Collections.Frozen;

namespace JoinRpg.WebComponents;

/// <summary>
/// Конфигурация пресетов кнопок (label/icon/style). Вынесено из <see cref="JoinButton"/>,
/// чтобы переиспользовать в других компонентах (например <see cref="JoinIconButton"/>).
/// </summary>
internal static class ButtonPresets
{
    internal static readonly FrozenDictionary<ButtonPreset, ButtonPresetContent> All = new Dictionary<ButtonPreset, ButtonPresetContent>
    {
        { ButtonPreset.None, new(null, null) },
        { ButtonPreset.Add, new("Добавить", "Добавляем...", "glyphicon-plus") },
        { ButtonPreset.Ok, new("Ok", null) },
        { ButtonPreset.Yes, new("Да",  null) },
        { ButtonPreset.No, new("Нет", null) },
        { ButtonPreset.Cancel, new("Отменить", "Отменяем...") },
        { ButtonPreset.Create, new("Создать", "Создаем...", "glyphicon-ok", VariationStyleEnum.Success) },
        { ButtonPreset.Delete, new("Удалить", "Удаляем...", "glyphicon-trash", VariationStyleEnum.Danger) },
        { ButtonPreset.Download, new("Скачать", "Скачиваем...", "glyphicon-cloud-download") },
        { ButtonPreset.Update, new("Обновить", "Обновляем...", "glyphicon-refresh") },
        { ButtonPreset.Hide, new("Скрыть", "Скрываем...", "glyphicon-remove-sign", VariationStyleEnum.Info) },
        { ButtonPreset.Restore, new("Восстановить", "Восстанавливаем...", style: VariationStyleEnum.Success) },
        { ButtonPreset.Print, new("Напечатать", "Печатаем...", "glyphicon-print") },
        { ButtonPreset.Email, new("Написать", "Отправляем письмо...", "glyphicon-envelope") },
        { ButtonPreset.Publish, new("Опубликовать", "Публикуем...", "glyphicon-share-alt", VariationStyleEnum.Success) },
        { ButtonPreset.Copy, new("Копировать", "Копируем...", "glyphicon-duplicate") },
        { ButtonPreset.Up, new(null, null, "glyphicon-arrow-up") },
        { ButtonPreset.Down, new(null, null, "glyphicon-arrow-down") },
        { ButtonPreset.Edit, new("Изменить", null, "glyphicon-pencil") },
        { ButtonPreset.Setup, new("Настройки", null, "glyphicon-cog") },
        { ButtonPreset.Save, new("Сохранить", "Сохраняем...", "glyphicon-ok", VariationStyleEnum.Success) },
        { ButtonPreset.Understand, new("Понятно", null) },
        { ButtonPreset.Logout, new("Выйти", "Выходим...", normalIcon: null, VariationStyleEnum.Danger) },
        { ButtonPreset.Login, new("Войти", "Входим...", normalIcon: null, VariationStyleEnum.Success) },
        { ButtonPreset.Vkontakte, new("ВКонтакте", "Входим...", normalIcon: null, VariationStyleEnum.Info) },
        { ButtonPreset.Unlink, new("Отвязать","Отвязываем...", "glyphicon-remove-sign", VariationStyleEnum.Info)},
        { ButtonPreset.Link, new("Привязать","Привязываем...", "glyphicon-plus", VariationStyleEnum.Info)},
        { ButtonPreset.Claim, new("Заявка",null, "glyphicon-file", VariationStyleEnum.Success)},
        { ButtonPreset.HotClaim, new("Заявиться",null, "glyphicon-fire", VariationStyleEnum.Warning)},
        { ButtonPreset.Profile, new("Профиль",null, "glyphicon-user", VariationStyleEnum.Info)},
        { ButtonPreset.Donate, new("Отослать донат",null, "glyphicon-usd", VariationStyleEnum.Success)},
        { ButtonPreset.Sort, new("Переместить", "Перемещаем...", "glyphicon-sort") },
    }.ToFrozenDictionary();
}

internal record struct ButtonContent(string? Label, string? Icon);

internal record struct ButtonPresetContent(ButtonContent Normal, ButtonContent Progress, VariationStyleEnum Style)
{
    public ButtonPresetContent(
      string? normalLabel,
      string? progressLabel,
      string? normalIcon = null,
      VariationStyleEnum style = VariationStyleEnum.None,
      string? progressIcon = JoinButton.DefaultProgressIcon)
        : this(new(normalLabel, normalIcon), new(progressLabel, progressIcon ?? normalIcon), style)
    { }
}
