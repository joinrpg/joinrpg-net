using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Portal.Models;

public class MainMenuViewModel
{
    public required ProjectShortInfo[] ProjectLinks { get; set; }
    public required int? CurrentProjectId { get; set; }
    public required string? CurrentProjectName { get; set; }
}
