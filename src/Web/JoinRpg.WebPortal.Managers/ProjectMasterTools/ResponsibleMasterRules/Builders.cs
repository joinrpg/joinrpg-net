using JoinRpg.Common.WebComponents;
using JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;

namespace JoinRpg.WebPortal.Managers.ProjectMasterTools.ResponsibleMasterRules;

internal static class Builders
{
    internal static ResponsibleMasterRuleViewModel ToRespRuleViewModel(
        this CharacterGroupInfo group,
        ProjectMasterInfo master)
        => new(
            Id: group.Id.CharacterGroupId,
            GroupName: group.Name,
            MasterLink: new UserLinkViewModel(master.UserInfo));
}
