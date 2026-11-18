namespace JoinRpg.WebPortal.Models.FieldSetup;

/// <summary>
/// Base view class for dropdown value
/// </summary>
public abstract class GameFieldDropdownValueViewModelBase
{
    [Display(Name = "Значение"), Required]
    public string Label { get; set; }

    // ReSharper disable once Mvc.TemplateNotResolved
    [Display(Name = "Описание"), UIHint("MarkdownString")]
    public string Description { get; set; }

    // ReSharper disable once Mvc.TemplateNotResolved
    [Display(Name = "Описание для мастеров"), UIHint("MarkdownString")]
    public string MasterDescription { get; set; }

    [Display(Name = "Цена", Description = "Если это поле заполнено, то цена будет добавлена ко взносу")]
    public int Price { get; set; } = 0;

    [Display(Name = "Игрок может выбрать", Description = "Если снять эту галочку, то игрок не сможет выбрать этот вариант, только мастер")]
    public bool PlayerSelectable { get; set; } = true;

    [Display(Name = "Программный ID",
        Description = "Используется для передачи во внешние ИТ-системы игры, если они есть. Значение определяется программистами внешней системы. Игнорируйте это поле, если у вас на игре нет никакой ИТ-системы")]
    public string ProgrammaticValue { get; set; }

    public int ProjectId { get; set; }
    public int ProjectFieldId { get; set; }
    public string FieldName { get; private set; }
    public bool CanPlayerEditField { get; private set; }


    [Display(Name = "Длина тайм-слота (в минутах")]
    public int TimeSlotInMinutes { get; set; }

    [Display(Name = "Начало тайм-слота", Description = "По часовому поясу проекта")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime TimeSlotStartTime { get; set; }

    [ReadOnly(true)]
    public bool IsTimeField { get; private set; }


    public GameFieldDropdownValueViewModelBase(ProjectFieldInfo field)
    {
        ProjectId = field.Id.ProjectId;
        ProjectFieldId = field.Id.ProjectFieldId;
        PlayerSelectable = field.CanPlayerEdit;
        FillNotEditable(field);
    }

    /// <summary>
    /// Свойства поля, которые не приходят из формы: без них перерисованная после ошибки форма
    /// теряет заголовок, галочку «Игрок может выбрать» и поля таймслота
    /// </summary>
    public void FillNotEditable(ProjectFieldInfo field)
    {
        FieldName = field.Name;
        CanPlayerEditField = field.CanPlayerEdit;
        IsTimeField = field.IsTimeSlot;
    }

    public GameFieldDropdownValueViewModelBase() { }

    public TimeSlotOptions? GetTimeSlotRequest(bool isTimeSlot)
        => isTimeSlot ? new TimeSlotOptions(TimeSlotStartTime, TimeSlotInMinutes) : null;
}
