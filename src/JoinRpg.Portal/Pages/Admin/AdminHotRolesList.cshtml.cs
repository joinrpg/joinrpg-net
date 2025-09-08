using JoinRpg.Data.Interfaces;
using JoinRpg.Web.AdminTools;
using JoinRpg.Web.AdminTools.KogdaIgra;
using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

public class AdminHotRolesListModel(IHotCharactersRepository hotCharactersRepository, IKogdaIgraSyncClient kogdaIgraSyncClient) : PageModel
{
    public async Task OnGetAsync()
    {
        var hotCharacters = await hotCharactersRepository.GetHotCharactersFromAllProjects();
        var kogdaIgraCards = await kogdaIgraSyncClient.GetKogdaIgraCards([.. hotCharacters.SelectMany(c => c.KogdaIgraLinkedIds)]);
        HotRoles = [.. hotCharacters.Select(c => new AdminHotRoleViewModel(c, SelectKogdaIgraCard(c, kogdaIgraCards)))];
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
