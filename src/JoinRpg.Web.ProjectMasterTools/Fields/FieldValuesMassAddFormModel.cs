using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectMasterTools.Fields;

/// <summary>
/// Форма добавления нескольких значений поля: одна строка — одно значение
/// </summary>
public class FieldValuesMassAddFormModel
{
    [Display(Name = "Значения", Description = "Одна строчка — одно значение")]
    [Required(ErrorMessage = "Введите хотя бы одно значение")]
    public string ValuesToAdd { get; set; } = "";
}

/// <summary>
/// Запрос на добавление нескольких значений поля
/// </summary>
/// <param name="ValuesToAdd">Значения, по одному на строку</param>
public record FieldValuesMassAddRequest(ProjectFieldIdentification FieldId, string ValuesToAdd);
