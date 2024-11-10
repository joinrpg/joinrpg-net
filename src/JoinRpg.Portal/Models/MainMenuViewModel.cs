using JoinRpg.Data.Interfaces;

namespace JoinRpg.Portal.Models;

public class MainMenuViewModel
{
    public required ProjectHeaderDto[] ProjectLinks { get; set; }
    public required int? CurrentProjectId { get; set; }
    public required string? CurrentProjectName { get; set; }
}
