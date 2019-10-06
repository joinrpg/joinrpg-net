using System.ComponentModel;

namespace JoinRpg.Web.Models.Schedules
{
    public enum ScheduleConfigProblemsViewModel
    {
        //TODO поменять сообщение, когда сделаем настроечный экран
        [Description("Расписание не настроено для этого проекта, обратитесь в техподдержку")]
        FieldsNotSet,
        [Description("У полей, привязанных к расписанию, разная видимость. Измените настройки видимости полей (публичные/игрокам/мастерам) на одинаковые")]
        InconsistentVisibility,
        [Description("У вас нет доступа к расписанию данного проекта")]
        NoAccess,
    }
}
