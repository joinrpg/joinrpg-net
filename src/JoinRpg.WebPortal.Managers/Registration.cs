namespace JoinRpg.WebPortal.Managers;

public static class Registration
{
    public static IEnumerable<Type> GetTypes()
    {
        yield return typeof(ProjectListManager);
        yield return typeof(FieldSetupManager);
        yield return typeof(Schedule.SchedulePageManager);
        yield return typeof(Subscribe.SubscribeViewService);
        yield return typeof(CharacterGroupList.CharacteGroupListViewService);
        yield return typeof(CheckIn.CheckInViewService);
        yield return typeof(ProjectMasterTools.ResponsibleMasterRules.ResponsibleMasterRuleViewService);
    }
}
