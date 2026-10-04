using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Schedules;

/// <summary>
/// В проекте не настроено расписание: нет поля тайм-слотов или поля помещений.
/// </summary>
/// <remarks>
/// Штатная ситуация, а не сбой: страницы расписания проверяют её заранее
/// (см. CheckScheduleConfiguration в SchedulePageManager), сюда доходит только вызывающий без проверки.
/// </remarks>
public class ScheduleNotEnabledException(ProjectIdentification projectId)
    : JoinRpgProjectException(projectId, "Schedule not enabled");
