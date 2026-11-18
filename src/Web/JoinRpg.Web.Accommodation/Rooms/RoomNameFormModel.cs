namespace JoinRpg.Web.Accommodation.Rooms;

/// <summary>
/// Форма диалога «Добавление комнат» / «Изменение комнаты».
/// </summary>
/// <param name="currentName">Прежнее имя при переименовании; <c>null</c> — добавляем комнаты</param>
public class RoomNameFormModel(string? currentName) : IValidatableObject
{
    public string? CurrentName { get; } = currentName;

    public bool IsAdding => CurrentName is null;

    [Display(Name = "Номера комнат")]
    [Required(ErrorMessage = "Укажите номер комнаты")]
    public string Names { get; set; } = currentName ?? "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CurrentName is not null && Names.Trim() == CurrentName)
        {
            yield return new ValidationResult("Название не изменилось", [nameof(Names)]);
        }
    }
}
