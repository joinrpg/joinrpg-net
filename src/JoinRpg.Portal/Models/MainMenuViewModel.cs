using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Portal.Models;

public record class MainMenuViewModel(ProjectShortInfo[] ProjectLinks, string? CurrentProjectName);
