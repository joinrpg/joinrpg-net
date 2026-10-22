namespace JoinRpg.Services.Interfaces.Projects;

/// <summary>
/// Синхронизация расписания с «Расписанием конвентов» (roletime.ru).
/// </summary>
public class RoleTimeOptions
{
    /// <summary>
    /// Пользователь-робот, которого добавляют мастером в новые проекты-программы конвентов.
    /// Не задан — робота не добавляют (локально и на тестовых стендах его нет).
    /// </summary>
    /// <remarks>
    /// Число, а не <see cref="UserIdentification"/>: биндер конфигурации не собирает его из скалярного значения.
    /// </remarks>
    public int? RobotUserId { get; set; }

    public UserIdentification? RobotUser => RobotUserId is { } id ? new UserIdentification(id) : null;
}
