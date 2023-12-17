using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Models;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Menu;

public class MainMenuViewComponent(ICurrentUserAccessor currentUserAccessor, IProjectRepository projectRepository) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var viewModel = new MainMenuViewModel()
        {
            ProjectLinks = await GetProjectLinks(),
        };
        return View("MainMenu", viewModel);
    }

    private async Task<List<MainMenuProjectLinkViewModel>> GetProjectLinks()
    {
        var user = currentUserAccessor.UserIdOrDefault;
        if (user == null)
        {
            return [];
        }
        var projects = await projectRepository.GetAllMyProjectsAsync(user.Value);
        return projects.ToMainMenuLinkViewModels().ToList();
    }
}
