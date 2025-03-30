using JoinRpg.Helpers.Web;

namespace JoinRpg.Web.AdminTools;

public record class AdminHotRoleViewModel(string CharacterName, string ProjectName, JoinHtmlString CharacterDesc, JoinHtmlString ProjectDesc)
{
}
