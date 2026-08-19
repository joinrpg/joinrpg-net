using System.Collections.Frozen;

namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Конфигурация пресетов кнопок (label/icon/style). Вынесено из <see cref="JoinButton"/>,
/// чтобы переиспользовать в других компонентах (например <see cref="JoinIconButton"/>).
/// </summary>
internal static class ButtonPresets
{
    internal static readonly FrozenDictionary<ButtonPreset, ButtonPresetContent> All = new Dictionary<ButtonPreset, ButtonPresetContent>
    {
        { ButtonPreset.None, new(null, null) },
        { ButtonPreset.Add, new("Добавить", "Добавляем...", JoinIconType.Add) },
        { ButtonPreset.Ok, new("Ok", null) },
        { ButtonPreset.Yes, new("Да",  null) },
        { ButtonPreset.No, new("Нет", null) },
        { ButtonPreset.Cancel, new("Отменить", "Отменяем...") },
        { ButtonPreset.Create, new("Создать", "Создаем...", JoinIconType.Ok, VariationStyleEnum.Success) },
        { ButtonPreset.Delete, new("Удалить", "Удаляем...", JoinIconType.Delete, VariationStyleEnum.Danger) },
        { ButtonPreset.Download, new("Скачать", "Скачиваем...", JoinIconType.Download) },
        { ButtonPreset.Update, new("Обновить", "Обновляем...", JoinIconType.Refresh) },
        { ButtonPreset.Hide, new("Скрыть", "Скрываем...", JoinIconType.Hide, VariationStyleEnum.Info) },
        { ButtonPreset.Restore, new("Восстановить", "Восстанавливаем...", JoinIconType.Edit, VariationStyleEnum.Success) },
        { ButtonPreset.Print, new("Напечатать", "Печатаем...", JoinIconType.Print) },
        { ButtonPreset.Email, new("Написать", "Отправляем письмо...", JoinIconType.Email) },
        { ButtonPreset.Publish, new("Опубликовать", "Публикуем...", JoinIconType.Publish, VariationStyleEnum.Success) },
        { ButtonPreset.Copy, new("Копировать", "Копируем...", JoinIconType.Copy) },
        { ButtonPreset.Up, new(null, null, JoinIconType.MoveUp) },
        { ButtonPreset.Down, new(null, null, JoinIconType.MoveDown) },
        { ButtonPreset.Edit, new("Изменить", null, JoinIconType.Edit) },
        { ButtonPreset.Setup, new("Настройки", null, JoinIconType.Setup) },
        { ButtonPreset.Save, new("Сохранить", "Сохраняем...", JoinIconType.Ok, VariationStyleEnum.Success) },
        { ButtonPreset.Understand, new("Понятно", null) },
        { ButtonPreset.Logout, new("Выйти", "Выходим...", normalIcon: null, VariationStyleEnum.Danger) },
        { ButtonPreset.Login, new("Войти", "Входим...", normalIcon: null, VariationStyleEnum.Success) },
        { ButtonPreset.Vkontakte, new("ВКонтакте", "Входим...", normalIcon: null, VariationStyleEnum.Info) },
        { ButtonPreset.Unlink, new("Отвязать","Отвязываем...", JoinIconType.Remove, VariationStyleEnum.Info)},
        { ButtonPreset.Link, new("Привязать","Привязываем...", JoinIconType.Add, VariationStyleEnum.Info)},
        { ButtonPreset.Claim, new("Заявка",null, JoinIconType.Claim, VariationStyleEnum.Success)},
        { ButtonPreset.HotClaim, new("Заявиться",null, JoinIconType.Hot, VariationStyleEnum.Warning)},
        { ButtonPreset.Profile, new("Профиль",null, JoinIconType.User, VariationStyleEnum.Info)},
        { ButtonPreset.Donate, new("Отослать донат",null, JoinIconType.Money, VariationStyleEnum.Success)},
        { ButtonPreset.Sort, new("Переместить", "Перемещаем...", JoinIconType.Sort) },
    }.ToFrozenDictionary();
}

internal record struct ButtonContent(string? Label, JoinIconType? Icon);

internal record struct ButtonPresetContent(ButtonContent Normal, ButtonContent Progress, VariationStyleEnum Style)
{
    public ButtonPresetContent(
      string? normalLabel,
      string? progressLabel,
      JoinIconType? normalIcon = null,
      VariationStyleEnum style = VariationStyleEnum.None,
      JoinIconType? progressIcon = JoinButton.DefaultProgressIcon)
        : this(new(normalLabel, normalIcon), new(progressLabel, progressIcon ?? normalIcon), style)
    { }
}
