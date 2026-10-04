namespace JoinRpg.Web.ProjectMasterTools.Fields;

/// <summary>
/// Параметры острова массового добавления таймслотов
/// </summary>
/// <param name="TimeZoneId">IANA-идентификатор таймзоны, в которой мастер вводит дату и время</param>
/// <param name="DefaultDate">Дата, подставляемая в форму</param>
/// <param name="DefaultSlotMinutes">Длина слота, подставляемая в форму</param>
public record TimeSlotMassAddViewModel(
    ProjectFieldIdentification FieldId,
    string TimeZoneId,
    DateOnly DefaultDate,
    int DefaultSlotMinutes);

/// <summary>
/// Запрос на массовое добавление таймслотов
/// </summary>
/// <param name="TimeZoneId">IANA-идентификатор таймзоны, в которой заданы дата и время</param>
public record TimeSlotMassAddRequest(
    ProjectFieldIdentification FieldId,
    string? Prefix,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int TimeSlotInMinutes,
    int BreakInMinutes,
    string TimeZoneId);
