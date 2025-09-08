using JoinRpg.Data.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.AdminTools;

public record class AdminHotRoleViewModel(
    CharacterLinkSlimViewModel CharacterLink,
    ProjectLinkViewModel ProjectLink,
    MarkupString CharacterDesc,
    MarkupString ProjectDesc,
    KogdaIgraCardViewModel? KogdaIgraCard)
{
    public AdminHotRoleViewModel(CharacterWithProject c, KogdaIgraCardViewModel? kogdaIgraCard)
        : this(new CharacterLinkSlimViewModel(c, true), new ProjectLinkViewModel(c.CharacterId.ProjectId, c.ProjectName),
            c.CharacterDesc.TakeWords(50).ToHtmlString(), c.ProjectDesc.TakeWords(50).ToHtmlString(), kogdaIgraCard)
    {

    }
}
