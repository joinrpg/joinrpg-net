using System.ComponentModel;

namespace JoinRpg.Web.Models.Schedules;

public enum ScheduleConfigProblemsViewModel
{
    [Description("Расписание не настроено для этого проекта. Вам необходимо добавить поля для помещения и расписания.")]
    FieldsNotSet,
    [Description("У полей, привязанных к расписанию, разная видимость. Измените настройки видимости полей (публичные/игрокам/мастерам) на одинаковые.")]
    InconsistentVisibility,
    [Description("У вас нет доступа к расписанию данного проекта")]
    NoAccess,

    [Description("Не настроено ни одного помещения")]
    NoRooms,
    [Description("Не настроено ни одного тайм-слота")]
    NoTimeSlots,
    [Description("Проект не найден")]
    ProjectNotFound,
}
