using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Models;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Menu;

public class MainMenuViewComponent : ViewComponent
{
    public MainMenuViewComponent(ICurrentUserAccessor currentUserAccessor, IProjectRepository projectRepository)
    {
        CurrentUserAccessor = currentUserAccessor;
        ProjectRepository = projectRepository;
    }

    private ICurrentUserAccessor CurrentUserAccessor { get; }
    private IProjectRepository ProjectRepository { get; }

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
        var user = CurrentUserAccessor.UserIdOrDefault;
        if (user == null)
        {
            return new List<MainMenuProjectLinkViewModel>();
        }
        var projects = await ProjectRepository.GetAllMyProjectsAsync(user.Value);
        return projects.ToMainMenuLinkViewModels().ToList();
    }
}
