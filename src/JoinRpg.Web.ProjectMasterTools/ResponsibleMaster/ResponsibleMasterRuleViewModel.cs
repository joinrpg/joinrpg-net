using JoinRpg.WebComponents;

namespace JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;

public record ResponsibleMasterRuleViewModel
    (int Id, string GroupName, UserLinkViewModel MasterLink)
{
    public static ResponsibleMasterRuleViewModel CreateSpecial(UserLinkViewModel masterLink)
        => new(-1, "", masterLink);

    public bool IsSpecial => Id == -1;
}
