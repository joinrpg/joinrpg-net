using JoinRpg.Data.Interfaces;
using JoinRpg.Helpers.Web;
using JoinRpg.Markdown;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.AdminTools;

public record class AdminHotRoleViewModel(CharacterLinkSlimViewModel CharacterLink, string ProjectName, JoinHtmlString CharacterDesc, JoinHtmlString ProjectDesc)
{
    public AdminHotRoleViewModel(CharacterWithProject c)
        : this(new CharacterLinkSlimViewModel(c, true), c.ProjectName,
            c.CharacterDesc.TakeWords(50).ToHtmlString(), c.ProjectDesc.TakeWords(50).ToHtmlString())
    {

    }
}
