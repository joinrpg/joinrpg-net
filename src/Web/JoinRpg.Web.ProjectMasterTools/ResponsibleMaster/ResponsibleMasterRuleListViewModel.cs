namespace JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;

public record ResponsibleMasterRuleListViewModel(
    List<ResponsibleMasterRuleViewModel> Items,
    bool HasEditAccess)
{
}
