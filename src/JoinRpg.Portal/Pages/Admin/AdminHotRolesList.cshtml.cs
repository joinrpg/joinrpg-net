using JoinRpg.Data.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.AdminTools;
using JoinRpg.Web.AdminTools.KogdaIgra;
using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.WebComponents;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

public class AdminHotRolesListModel(IHotCharactersRepository hotCharactersRepository, IKogdaIgraSyncClient kogdaIgraSyncClient) : PageModel
{
    public async Task OnGetAsync()
    {
        var hotCharacters = await hotCharactersRepository.GetHotCharactersFromAllProjects();
        var kogdaIgraCards = await kogdaIgraSyncClient.GetKogdaIgraCards([.. hotCharacters.SelectMany(c => c.KogdaIgraLinkedIds)]);
        HotRoles = [.. hotCharacters.Select(dto => ToAdminHotRoleViewModel(dto, kogdaIgraCards))];
    }

    private static AdminHotRoleViewModel ToAdminHotRoleViewModel(CharacterWithProject dto, KogdaIgraCardViewModel[] kogdaIgraCards)
    {
        return new AdminHotRoleViewModel(
                    new CharacterLinkSlimViewModel(dto.CharacterId, dto.CharacterName, dto.IsActive, ViewModeSelector.Create(dto.IsPublic, canViewPrivate: true)),
                    new ProjectLinkViewModel(dto.CharacterId.ProjectId, dto.ProjectName),
                    dto.CharacterDesc.TakeWords(50).ToHtmlString(), dto.ProjectDesc.TakeWords(50).ToHtmlString(),
                    SelectKogdaIgraCard(dto, kogdaIgraCards));
    }

    private static KogdaIgraCardViewModel? SelectKogdaIgraCard(CharacterWithProject c, KogdaIgraCardViewModel[] kogdaIgraCards)
    {
        var candidates = kogdaIgraCards.Where(k => c.KogdaIgraLinkedIds.Contains(k.KogdaIgraId)).ToList();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (candidates.Where(x => x.Begin > today).OrderBy(x => x.Begin).FirstOrDefault() is KogdaIgraCardViewModel nearestFuture)
        {
            return nearestFuture;
        }
        return candidates.OrderByDescending(x => x.Begin).FirstOrDefault();
    }

    public IReadOnlyCollection<AdminHotRoleViewModel> HotRoles { get; set; } = null!;
}
