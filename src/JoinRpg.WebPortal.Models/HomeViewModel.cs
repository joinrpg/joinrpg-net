using JoinRpg.Web.Games.Projects;

namespace JoinRpg.WebPortal.Models;

public class HomeViewModel
{
    public required IReadOnlyCollection<ProjectListItemViewModel> AllProjects { get; set; }
    public required IReadOnlyCollection<ProjectListItemViewModel> MyProjects { get; set; }
    public required bool HasMoreProjects { get; set; }
}
