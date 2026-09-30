namespace JoinRpg.Domain.Schedules;

public static class ScheduleFieldExtensions
{
    /// <summary>
    /// Активное специальное поле расписания заданного типа. Таких полей в проекте не больше
    /// одного — это и проверяется при создании поля.
    /// </summary>
    public static ProjectField? GetScheduleFieldOrDefault(this Project project, ProjectFieldType fieldType)
        => project.ProjectFields.SingleOrDefault(f => f.FieldType == fieldType && f.IsActive);

    public static ProjectField? GetTimeSlotFieldOrDefault(this Project project)
        => project.GetScheduleFieldOrDefault(ProjectFieldType.ScheduleTimeSlotField);

    public static ProjectField? GetRoomFieldOrDefault(this Project project)
        => project.GetScheduleFieldOrDefault(ProjectFieldType.ScheduleRoomField);
}
