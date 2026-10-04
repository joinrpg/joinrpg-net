namespace JoinRpg.Web.ProjectMasterTools.Fields;

/// <summary>
/// Параметры острова массового добавления таймслотов. Дата и время — на часах в часовом поясе проекта.
/// </summary>
/// <param name="DefaultDate">Дата, подставляемая в форму</param>
/// <param name="DefaultSlotMinutes">Длина слота, подставляемая в форму</param>
public record TimeSlotMassAddViewModel(
    ProjectFieldIdentification FieldId,
    DateOnly DefaultDate,
    int DefaultSlotMinutes);

/// <summary>
/// Запрос на массовое добавление таймслотов. Дата и время — на часах в часовом поясе проекта.
/// </summary>
public record TimeSlotMassAddRequest(
    ProjectFieldIdentification FieldId,
    string? Prefix,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int TimeSlotInMinutes,
    int BreakInMinutes);
