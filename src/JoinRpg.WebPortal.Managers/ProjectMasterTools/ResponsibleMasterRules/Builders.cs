using JoinRpg.DataModel;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;

namespace JoinRpg.WebPortal.Managers.ProjectMasterTools.ResponsibleMasterRules;
internal static class Builders
{
    internal static ResponsibleMasterRuleViewModel ToRespRuleViewModel(
        this CharacterGroup characterGroup
        )
    {
        if (characterGroup.ResponsibleMasterUser is null)
        {
            throw new ArgumentNullException("characterGroup.ResponsibleMasterUser");
        }
        return new ResponsibleMasterRuleViewModel(
            Id: characterGroup.CharacterGroupId,
            GroupName: characterGroup.CharacterGroupName,
            MasterLink: UserLinks.Create(characterGroup.ResponsibleMasterUser));
    }
}
