namespace JoinRpg.WebComponents;

/// <summary>
/// Иконка, доступная в интерфейсе Joinrpg.
/// </summary>
/// <remarks>
/// Значения именуются по смыслу («что означает иконка»), а не по внешнему виду.
/// Поэтому несколько значений могут отображаться одним и тем же глифом — это нормально:
/// при смене иконочного набора они разойдутся. Соответствие значения и глифа задаётся
/// в единственном месте — <see cref="JoinIconDefinitions"/>.
/// </remarks>
public enum JoinIconType
{
    /// <summary>Добавить (+).</summary>
    Add,
    /// <summary>Подтверждение, галочка.</summary>
    Ok,
    /// <summary>Удалить.</summary>
    Delete,
    /// <summary>Скачать файл.</summary>
    Download,
    /// <summary>Выгрузить/экспортировать данные.</summary>
    Export,
    /// <summary>Обновить.</summary>
    Refresh,
    /// <summary>Скрыть.</summary>
    Hide,
    /// <summary>Проект закрыт (в архиве).</summary>
    Closed,
    /// <summary>Напечатать.</summary>
    Print,
    /// <summary>Электронная почта.</summary>
    Email,
    /// <summary>Опубликовать.</summary>
    Publish,
    /// <summary>Копировать, вторая роль.</summary>
    Copy,
    /// <summary>Переместить вверх.</summary>
    MoveUp,
    /// <summary>Переместить вниз.</summary>
    MoveDown,
    /// <summary>Изменить.</summary>
    Edit,
    /// <summary>Настройки.</summary>
    Setup,
    /// <summary>Инструменты, настроить объект.</summary>
    Tools,
    /// <summary>Заявка.</summary>
    Claim,
    /// <summary>Горячая роль.</summary>
    Hot,
    /// <summary>Пользователь, профиль.</summary>
    User,
    /// <summary>Деньги, взнос.</summary>
    Money,
    /// <summary>Сортировка, перемещение.</summary>
    Sort,
    /// <summary>Сортировка по алфавиту.</summary>
    SortByAlphabet,
    /// <summary>Операция в процессе.</summary>
    Progress,
    /// <summary>Информация.</summary>
    Info,
    /// <summary>Подписка.</summary>
    Subscribe,
    /// <summary>Предупреждение.</summary>
    Warning,
    /// <summary>Важное замечание.</summary>
    Alert,
    /// <summary>Убрать, крестик.</summary>
    Remove,
    /// <summary>Поселение, комната.</summary>
    Accommodation,
    /// <summary>Освободить комнату.</summary>
    Evict,
    /// <summary>Выселить всех.</summary>
    EvictAll,
    /// <summary>Взнос оплачен.</summary>
    FeePaid,
    /// <summary>Телеграм.</summary>
    Telegram,
    /// <summary>Помощь.</summary>
    Help,
    /// <summary>Телефон.</summary>
    Phone,
    /// <summary>Принять (приглашение, предложение).</summary>
    Approve,
    /// <summary>Отклонить или отозвать (приглашение, предложение).</summary>
    Decline,
    /// <summary>Назад.</summary>
    Back,
    /// <summary>Поиск.</summary>
    Search,
    /// <summary>Календарь, дата.</summary>
    Calendar,
    /// <summary>Видимо игроку.</summary>
    Visible,
    /// <summary>Скрыто от игрока.</summary>
    Hidden,
    /// <summary>Отмеченный чекбокс.</summary>
    CheckboxChecked,
    /// <summary>Неотмеченный чекбокс.</summary>
    CheckboxUnchecked,
    /// <summary>Меню (вертикальное многоточие).</summary>
    MenuVertical,
    /// <summary>Меню (горизонтальное многоточие).</summary>
    MenuHorizontal,
    /// <summary>Право доступа есть.</summary>
    HasAccess,
    /// <summary>Права доступа нет.</summary>
    NoAccess,
    /// <summary>Переместить заявку.</summary>
    Move,
    /// <summary>Регистрация на полигоне.</summary>
    CheckIn,
    /// <summary>Перейти к первой версии.</summary>
    GoToFirst,
    /// <summary>Перейти к предыдущей версии.</summary>
    GoToPrev,
    /// <summary>Перейти к следующей версии.</summary>
    GoToNext,

    /// <summary>Поле обязательно к заполнению (красная звёздочка).</summary>
    MandatoryStar,
    /// <summary>Поле рекомендуется заполнить (синяя звёздочка).</summary>
    RecommendedStar,
    /// <summary>Закрыть окно.</summary>
    Close,
    /// <summary>Вопрос в диалоге.</summary>
    DialogQuestion,
    /// <summary>Информация в диалоге.</summary>
    DialogInfo,
    /// <summary>Успех в диалоге.</summary>
    DialogSuccess,
    /// <summary>Предупреждение в диалоге.</summary>
    DialogAlert,
    /// <summary>Неудача в диалоге.</summary>
    DialogFail,

    /// <summary>ВКонтакте.</summary>
    Vk,
}
