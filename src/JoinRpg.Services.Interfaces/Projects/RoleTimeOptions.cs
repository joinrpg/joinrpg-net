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
    public UserIdentification? RobotUserId { get; set; }
}
