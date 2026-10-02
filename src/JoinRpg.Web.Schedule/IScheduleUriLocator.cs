namespace JoinRpg.Web.Schedule;

/// <summary>
/// Ссылки на страницы расписания проекта.
/// </summary>
public interface IScheduleUriLocator
{
    /// <summary>
    /// Расписание в обычном виде — внутри общего layout сайта, с меню проекта.
    /// </summary>
    Uri GetScheduleUri(ProjectIdentification projectId);
}
