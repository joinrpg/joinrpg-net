using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.AdminTools;

public record class AdminHotRoleViewModel(
    CharacterLinkSlimViewModel CharacterLink,
    ProjectLinkViewModel ProjectLink,
    MarkupString CharacterDesc,
    MarkupString ProjectDesc,
    KogdaIgraCardViewModel? KogdaIgraCard);
