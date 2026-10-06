namespace JoinRpg.Web.Models;

/// <summary>
/// Форма добавления мастера: права и профиль (ADR019, §4). Правка профиля потом — отдельно, на странице мастера.
/// </summary>
public class AddAclViewModel : ChangeAclViewModel
{
    /// <summary>Роль, которой предзаполнена форма добавления мастера.</summary>
    public const string DefaultRole = "Мастер";

    // Без значения по умолчанию: POST без поля должен падать на валидации, а не молча ставить роль.
    // Предзаполнение формы — в AclViewModel.
    [Required(ErrorMessage = "Укажите роль мастера в проекте")]
    [MaxLength(100)]
    public string? Role { get; set; }

    public string? Description { get; set; }

    public bool IsPublic { get; set; }
}
