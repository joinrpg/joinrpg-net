using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Schedules;

namespace JoinRpg.Web.Models.FieldSetup;

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
    public string FieldName { get; }
    public bool CanPlayerEditField { get; }


    [Display(Name = "Длина тайм-слота (в минутах")]
    public int TimeSlotInMinutes { get; set; }

    [Display(Name = "Начало тайм-слота", Description = "В формате ГГГГ-ММ-ДДTЧЧ:ММ+03:00. Если таймзона не указывается, подразумевается московское.")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mmK}", ApplyFormatInEditMode = true)]
    public DateTimeOffset TimeSlotStartTime { get; set; }

    [ReadOnly(true)]
    public bool IsTimeField { get; set; }


    public GameFieldDropdownValueViewModelBase(ProjectField field)
    {
        FieldName = field.FieldName;
        ProjectId = field.ProjectId;
        ProjectFieldId = field.ProjectFieldId;
        PlayerSelectable = CanPlayerEditField = field.CanPlayerEdit;
        IsTimeField = field.IsTimeSlot();
    }

    public GameFieldDropdownValueViewModelBase() { }

    public TimeSlotOptions? GetTimeSlotRequest(ProjectField field, string? value)
    {
        return value is not null && field.IsTimeSlot()
            ? new TimeSlotOptions
            {
                StartTime = DateTimeOffset.ParseExact(
                    value,
                    "yyyy-MM-ddTHH:mmK",
                    System.Globalization.CultureInfo.InvariantCulture),
                TimeSlotInMinutes = TimeSlotInMinutes
            }
            : null;
    }
}
