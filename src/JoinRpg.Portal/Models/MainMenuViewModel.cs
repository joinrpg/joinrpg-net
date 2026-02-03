using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Portal.Models;

public record class MainMenuViewModel(ProjectPersonalizedInfo[] ProjectLinks, string? CurrentProjectName);
