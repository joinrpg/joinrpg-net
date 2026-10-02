namespace JoinRpg.Web.Schedule;

/// <summary>
/// Ссылки на страницы расписания проекта. Реализаций две и они независимы — серверная
/// (<c>UriServiceImpl</c>) через <c>LinkGenerator</c> и клиентская (<c>UriLocatorExtensions</c>)
/// строкой; что они не разошлись, проверяет <c>UriLocatorConsistencyTests</c>.
/// </summary>
public interface IScheduleUriLocator
{
    /// <summary>
    /// Расписание в обычном виде — внутри общего layout сайта, с меню проекта.
    /// </summary>
    Uri GetScheduleUri(ProjectIdentification projectId);
}
