using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectMasterTools.Fields;

/// <summary>
/// Форма массового добавления таймслотов
/// </summary>
public class TimeSlotMassAddFormModel : IValidatableObject
{
    /// <summary>
    /// Границы игрового дня, подставляемые в форму по умолчанию
    /// </summary>
    public static readonly TimeOnly DefaultStartTime = new(10, 0);
    public static readonly TimeOnly DefaultEndTime = new(22, 0);

    [Display(Name = "Префикс", Description = "Добавляется перед временем в названии значения, например «Зал 1 10:00–10:50»")]
    public string Prefix { get; set; } = "";

    [Display(Name = "Дата"), Required(ErrorMessage = "Укажите дату")]
    public DateOnly? Date { get; set; }

    [Display(Name = "Время начала"), Required(ErrorMessage = "Укажите время начала")]
    public TimeOnly? StartTime { get; set; }

    [Display(Name = "Время конца", Description = "Если конец раньше начала, он считается на следующий день")]
    [Required(ErrorMessage = "Укажите время конца")]
    public TimeOnly? EndTime { get; set; }

    [Display(Name = "Длина таймслота (в минутах)"), Required(ErrorMessage = "Укажите длину таймслота")]
    [Range(1, TimeSlotBatch.MaxMinutes, ErrorMessage = "Длина таймслота — от 1 минуты до суток")]
    public int? SlotMinutes { get; set; }

    [Display(Name = "Перерыв между таймслотами (в минутах)"), Required(ErrorMessage = "Укажите перерыв")]
    [Range(0, TimeSlotBatch.MaxMinutes, ErrorMessage = "Перерыв — от 0 минут до суток")]
    public int? BreakMinutes { get; set; }

    public bool EndsNextDay => StartTime is not null && EndTime is not null && EndTime < StartTime;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime is not null && StartTime == EndTime)
        {
            yield return new ValidationResult("Время конца должно отличаться от времени начала", [nameof(EndTime)]);
        }
    }

    /// <summary>
    /// Нарезать слоты. Вызывать только для валидной формы.
    /// </summary>
    public IReadOnlyList<TimeSlotBatchItem> Generate()
        => TimeSlotBatch.Generate(Prefix, Date!.Value, StartTime!.Value, EndTime!.Value, SlotMinutes!.Value, BreakMinutes!.Value);
}
